using System.Reflection;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Autofac;
using MediatR;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Extensions.Autofac.DependencyInjection;
using Serilog.Sinks.SystemConsole.Themes;
using Wordle.ActiveMq.Consumer;
using Wordle.ActiveMq.Publisher;
using Wordle.Api.Common;
using Wordle.Clock;
using Wordle.Common;
using Wordle.Dictionary;
using Wordle.Dictionary.EfCore;
using Wordle.EfCore;
using Wordle.Events;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Persistence.EfCore;
using Wordle.QueryHandlers.EfCore;
using Wordle.Render;
using static Wordle.Common.Actions;

namespace Wordle.Apps.Common;

public class AutofacConfigurationBuilder
{
    private readonly ContainerBuilder _builder;

    private static readonly List<Assembly> MediatrAssemblies =
    [
        // we assume these are always required as we will be generating
        typeof(Wordle.CommandHandlers.Usings).Assembly,
    ];
    
    private static readonly Action<ContainerBuilder> AddS3Client = callOnlyOnce<ContainerBuilder>((b) =>
    {
        b.RegisterInstance(new AmazonS3Client()).As<IAmazonS3>().SingleInstance();
    });

    private static readonly Action<ContainerBuilder> InitialiseDefaultsCallback = callOnlyOnce<ContainerBuilder>((b) =>
    {
        b.RegisterSerilog(new LoggerConfiguration()
            .WriteTo
            .Console(theme: AnsiConsoleTheme.Grayscale));

        b.RegisterType<Clock.Clock>().As<IClock>().SingleInstance();
        b.RegisterType<GuessDecimator>().As<IGuessDecimator>().SingleInstance();

//        AddSqsClient(b);
//        AddSnsClient(b);

        // add all of the mediatr stuff, as it's pretty much always used - but allow the other builder calls to determine
        // which mediatr things should be registered as they can be different for different apps
        var configuration = MediatRConfigurationBuilder
            .Create(
                MediatrAssemblies.ToArray()
            )
            .UseNotificationPublisher(typeof(TaskWhenAllPublisher))
            .WithAllOpenGenericHandlerTypesRegistered()
            .WithRegistrationScope(RegistrationScope.Scoped)
            .Build();

        b.RegisterMediatR(configuration);
    });

    public AutofacConfigurationBuilder(ContainerBuilder? i = null)
    {
        _builder = i ?? new ContainerBuilder();
    }

    public void InitialiseDefaults()
    {
        InitialiseDefaultsCallback(_builder);
    }

    public AutofacConfigurationBuilder AddPostgresPersistence(bool autoMigrate = true)
    {
        MediatrAssemblies.Add(typeof(Usings).Assembly);

        if (autoMigrate)
        {
            _builder.RegisterType<Wordle.EfCore.WordleMigrator>()
                .As<IStartable>()
                .SingleInstance();
        }

        _builder
            .RegisterType<WordleEfCoreSettings>()
            .As<WordleEfCoreSettings>()
            .WithParameter(new PositionalParameter(0, EnvironmentVariables.PostgresConnectionString));

        _builder.RegisterType<EfUnitOfWorkFactory>()
            .As<IGameUnitOfWorkFactory>()
            .SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddPostgresDictionary(bool autoMigrate = true)
    {
        if (autoMigrate)
        {
            _builder.RegisterType<Wordle.Dictionary.EfCore.EfMigrator>()
                .As<IStartable>()
                .SingleInstance();
        }

        _builder
            .RegisterType<DictionaryEfCoreSettings>()
            .As<DictionaryEfCoreSettings>()
            .WithParameter(new PositionalParameter(0, EnvironmentVariables.PostgresConnectionString));

        _builder
            .RegisterType<EfCoreDictionaryService>()
            .As<IWordleDictionaryService>()
            .SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddS3ImagePersistence()
    {
        AddS3Client(_builder);
        _builder.RegisterType<Renderer>().As<IRenderer>().SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddLocalDiskImagePersistence()
    {
        _builder.RegisterType<Renderer>().As<IRenderer>().SingleInstance();

        return this;
    }


    
    public AutofacConfigurationBuilder AddActiveMqEventPublisher(string instanceType, string instanceId, bool useHostedService = false)
    {
        _builder.RegisterInstance(new ActiveMqEventPublisherOptions()
            {
                ActiveMqUri = EnvironmentVariables.ActiveMqBrokerUrl,
                InstanceType = instanceType,
                InstanceId = instanceId
            })
            .As<ActiveMqEventPublisherOptions>()
            .SingleInstance();

        _builder.RegisterType<ActiveMqEventPublisherService>()
            .As<IEventPublisherService>()
            .SingleInstance();
        
        // use this to capture all events and then send them out to the event publisher service
        _builder
            .RegisterType<EventPublisherServiceDecoratorImpl>()
            .AsImplementedInterfaces()
            .SingleInstance();

        if (useHostedService)
        {
            _builder.RegisterType<EventPublisherBackgroundService>()
                .As<IHostedService>()
                .SingleInstance();
        }

        return this;
    }

    public AutofacConfigurationBuilder AddActiveMqEventConsumer(string instanceType, string instanceId, bool useHostedService = false)
    {
        var settings = new ActiveMqEventConsumerOptions()
        {
            ActiveMqUri = EnvironmentVariables.ActiveMqBrokerUrl,
            InstanceType = instanceType,
            InstanceId = instanceId
        };

        _builder.RegisterInstance(settings)
            .As<ActiveMqEventConsumerOptions>()
            .OnActivating(x =>
            {
                var handlers = x.Context
                    .ComponentRegistry
                    .Registrations
                    .SelectMany(x =>
                    {
                        var type = x.Activator.LimitType;
                        if (!type.IsClass)
                        {
                            return [];
                        }

                        // this is a little dodgy, but because we do event publishing by hooking into the same mechanism
                        // that we use to receive other events the inclusion of this interface basically means that we will
                        // always need all events - which really isn't ideal, so we intentionally remove this interface from the
                        // list.
                        // TODO : validate this, there may eventually be a case where we have a legitimate need, but so far
                        // things are okay.
                        if (type.GetInterfaces().Any(x => typeof(IAllEventHandlers).IsAssignableFrom(x)))
                        {
                            return [];
                        }

                        // this logic is a little complex, so here is an explanation:
                        // We want to go through all registered components, find any that implement the INotificationHandler interface
                        // and then get the type of the argument to the interface definition from there as long as they also implement
                        // the IEvent interface (and are classes given above) that now gives us a set of the events that this application
                        // is interested in receiving events for - which also limits the amount of connections to the underlying transport
                        // we need to make (as we only listen to the events we know we want to receive.
                        return type.GetInterfaces()
                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                            .SelectMany(x =>
                            {
                                var events = x.GetGenericArguments()
                                    .Where(x => typeof(IEvent).IsAssignableFrom(x))
                                    .Select(x => x).ToList();
                                
                                return events;
                            })
                            .ToList();
                    })
                    .Distinct()
                    .ToList();

                settings.EventTypesToMonitor = handlers;
            })
            .SingleInstance();

        _builder.RegisterType<ActiveMqEventConsumerService>()
            .As<IEventConsumerService>()
            .SingleInstance();

        if (useHostedService)
        {
            _builder.RegisterType<EventConsumerBackgroundService>()
                .As<IHostedService>()
                .SingleInstance();
        }

        return this;
    }

    public AutofacConfigurationBuilder AddRenderer()
    {
        _builder.RegisterInstance(new Renderer()).As<IRenderer>();
        return this;
    }


    public IContainer Build()
    {
        // if it hasn't been called already then we should defo call it
        InitialiseDefaultsCallback(_builder);
        return _builder.Build();
    }

    public AutofacConfigurationBuilder Callback(Action<ContainerBuilder> action)
    {
        action(_builder);
        return this;
    }

    public AutofacConfigurationBuilder RegisterSelf(Type program, bool inclueMediatr = true)
    {
        _builder.RegisterType(program).As(program).SingleInstance();

        if (inclueMediatr)
        {
            MediatrAssemblies.Add(program.Assembly);
        }

        return this;
    }
}