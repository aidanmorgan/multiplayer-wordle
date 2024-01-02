using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Autofac.DependencyInjection;
using Serilog.Sinks.SystemConsole.Themes;
using Wordle.Aws.Common;
using Wordle.Aws.DictionaryImpl;
using Wordle.Aws.EventBridge;
using Wordle.Kafka.Consumer;
using Wordle.Kafka.Publisher;
using Wordle.Clock;
using Wordle.Dictionary;
using Wordle.Dictionary.EfCore;
using Wordle.EfCore;
using Wordle.Kafka.Common;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Persistence.Dynamo;
using Wordle.Persistence.EfCore;
using Wordle.Redis.Common;
using Wordle.Redis.Consumer;
using Wordle.Redis.Publisher;
using Wordle.Render;
using EfMigrator = Wordle.EfCore.EfMigrator;

namespace Wordle.Apps.Common;

public class AutofacConfigurationBuilder
{
    private readonly ContainerBuilder _builder;
    
    private static readonly List<Assembly> MediatrAssemblies =
    [
        // we assume these are always required as we will be generating
        typeof(Wordle.CommandHandlers.Usings).Assembly,
    ];
    
    private static readonly Action<ContainerBuilder> AddDynamoDbClient = callOnlyOnce((b) =>
    { 
        b.RegisterInstance(new AmazonDynamoDBClient()).As<IAmazonDynamoDB>().SingleInstance();
    });

    private static readonly Action<ContainerBuilder> AddS3Client = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonS3Client()).As<IAmazonS3>().SingleInstance();
    });

    private static readonly Action<ContainerBuilder> AddEventBridgeClient = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonEventBridgeClient()).As<IAmazonEventBridge>().SingleInstance();
    });

    private static readonly Action<ContainerBuilder> AddSqsClient = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonSQSClient()).As<IAmazonSQS>().SingleInstance();
    });

    private static readonly Action<ContainerBuilder> AddSnsClient = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonSimpleNotificationServiceClient()).As<IAmazonSimpleNotificationService>();
    });

    private static readonly Action<ContainerBuilder> InitialiseDefaultsCallback = callOnlyOnce((b) =>
    {
        b.RegisterSerilog(new LoggerConfiguration()
            .WriteTo
            .Console(theme: AnsiConsoleTheme.Grayscale));
        
        b.RegisterType<Clock.Clock>().As<IClock>().SingleInstance();
        b.RegisterType<GuessDecimator>().As<IGuessDecimator>().SingleInstance();

        AddSqsClient(b);
        AddSnsClient(b);

        // add all of the mediatr stuff, as it's pretty much always used
        var configuration = MediatRConfigurationBuilder
            .Create(
                MediatrAssemblies.ToArray()
            )
            .UseNotificationPublisher(typeof(TaskWhenAllPublisher))
            .WithAllOpenGenericHandlerTypesRegistered()
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
    
    public AutofacConfigurationBuilder AddDynamoPersistence()
    {
        AddDynamoDbClient(_builder);
        
        MediatrAssemblies.Add(typeof(Wordle.QueryHandlers.Dynamo.Usings).Assembly);
        
        _builder.RegisterType<DynamoGameConfiguration>()
            .As<DynamoGameConfiguration>()
            .WithParameter(new TypedParameter(typeof(string),
                EnvironmentVariables.GameDynamoTableName))
            .SingleInstance();

        _builder.RegisterInstance(new DynamoMappers()).As<IDynamoMappers>().SingleInstance();
        _builder.RegisterType<DynamoGameUnitOfWorkFactory>().As<IGameUnitOfWorkFactory>().SingleInstance();
        
        return this;
    }

    public AutofacConfigurationBuilder AddPostgresPersistence()
    {
        MediatrAssemblies.Add(typeof(Wordle.QueryHandlers.EntityFramework.Usings).Assembly);

        _builder.RegisterType<Wordle.EfCore.EfMigrator>()
            .As<IStartable>()
            .SingleInstance();
        
        _builder
            .RegisterType<WordleContext>()
            .As<WordleContext>()
            .WithParameter(new PositionalParameter(0, EnvironmentVariables.PostgresConnectionString));

        _builder.RegisterType<EfUnitOfWorkFactory>()
            .As<IGameUnitOfWorkFactory>()
            .WithParameter(new PositionalParameter(0, EnvironmentVariables.PostgresConnectionString))
            .SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddDynamoDictionary()
    {
        AddDynamoDbClient(_builder);

        _builder.RegisterType<DynamoDictionaryConfiguration>()
            .As<DynamoDictionaryConfiguration>()
            .WithParameter(new TypedParameter(typeof(string),
                EnvironmentVariables.DictionaryDynamoTableName))
            .SingleInstance();

        _builder.RegisterType<DynamoDbWordleDictionaryService>()
            .As<IWordleDictionaryService>()
            .SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddPostgresDictionary()
    {
        _builder.RegisterType<Wordle.Dictionary.EfCore.EfMigrator>()
            .As<IStartable>()
            .SingleInstance();
        
        _builder
            .RegisterType<DictionaryContext>()
            .As<DictionaryContext>()
            .WithParameter(new PositionalParameter(0, EnvironmentVariables.PostgresConnectionString));

        return this;
    }

    public AutofacConfigurationBuilder AddImagePersistence()
    {
        AddS3Client(_builder);

        return this;
    }

    public AutofacConfigurationBuilder AddKafkaEventPublishing(string instanceType, string instanceId)
    {
        _builder.RegisterInstance(new KafkaSettings()
        {
            BootstrapServers = EnvironmentVariables.KafkaBootstrapServers,
            Topic = EnvironmentVariables.KafkaEventTopic,
            InstanceType = instanceType,
            InstanceId = instanceId,
        }).As<KafkaSettings>();
        
        _builder.RegisterType<KafkaEventPublisher>()
            .AsImplementedInterfaces();
        
        MediatrAssemblies.Add(typeof(KafkaEventPublisher).Assembly);

        return this;
    }
    
    public AutofacConfigurationBuilder AddKafkaEventConsuming(string instanceType, string instanceId)
    {
        _builder.RegisterInstance(new KafkaSettings()
        {
            BootstrapServers = EnvironmentVariables.KafkaBootstrapServers,
            Topic = EnvironmentVariables.KafkaEventTopic,
            InstanceType = instanceType,
            InstanceId = instanceId
        }).As<KafkaSettings>();
        
        _builder
            .RegisterType<KafkaEventConsumerService>()
            .AsImplementedInterfaces()
            .SingleInstance();
        
        MediatrAssemblies.Add(typeof(KafkaEventConsumerService).Assembly);
        
        return this;
    }
    
    public AutofacConfigurationBuilder AddEventBridgePublishing(string instanceType, string instanceId)
    {
        AddEventBridgeClient(_builder);
        
        _builder.RegisterType<EventBridgePublisher>()
            .AsImplementedInterfaces()
            .WithParameter(new PositionalParameter(1, EnvironmentVariables.EventBridgeName))
            
            // these are used to help with horizontal scaling, basically if we receive an event from the same instance
            // type then we will ignore it and not process it because it's meant to go somewhere else
            .WithParameter(new PositionalParameter(2, instanceType))
            .WithParameter(new PositionalParameter(3, instanceId))
            .SingleInstance();
        
        MediatrAssemblies.Add(typeof(EventBridgePublisher).Assembly);


        return this;
    }

    public AutofacConfigurationBuilder AddSqsEventConsuming(string url, string eventSource, string instanceId)
    {
        _builder.RegisterType<SqsEventConsumerService>().As<IEventConsumerService>()
            .WithParameter(new PositionalParameter(0, url))
            .WithParameter(new PositionalParameter(1, eventSource))
            .WithParameter(new PositionalParameter(2, instanceId))
            .AsImplementedInterfaces()
            .SingleInstance();
        
        MediatrAssemblies.Add(typeof(SqsEventConsumerService).Assembly);
        
        return this;
    }

    public AutofacConfigurationBuilder AddRedisEventPublisher(string instanceType, string instanceId)
    {
        _builder.RegisterInstance(new RedisSettings()
        {
            RedisHost = EnvironmentVariables.RedisServer,
            RedisTopic = EnvironmentVariables.RedisTopic,
            InstanceType = instanceType,
            InstanceId = instanceId
        }).As<RedisSettings>();

        // singleton instnce that performs the actual redis logic
        _builder
            .RegisterType<DefaultRedisPublisher>()
            .As<IRedisPublisher>()
            .SingleInstance()
            .OnActivated(x => x.Instance.Start());
        
        // wrapper that implements INotificationHandler that delegates to the IRedisPublisher
        _builder
            .RegisterType<RedisEventPublisher>()
            .AsImplementedInterfaces();
        
        MediatrAssemblies.Add(typeof(RedisEventPublisher).Assembly);

        return this;
    }

    public AutofacConfigurationBuilder AddRedisEventConsumer(string instanceType, string instanceId)
    {
        _builder.RegisterInstance(new RedisSettings()
        {
            RedisHost = EnvironmentVariables.RedisServer,
            RedisTopic = EnvironmentVariables.RedisTopic,
            InstanceType = instanceType,
            InstanceId = instanceId
        }).As<RedisSettings>();

        _builder
            .RegisterType<RedisEventConsumerService>()
            .As<IEventConsumerService>()
            .SingleInstance()
            .OnActivated(x => x.Instance.Start());
        
        MediatrAssemblies.Add(typeof(RedisEventConsumerService).Assembly);
        
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

    static Action<ContainerBuilder> callOnlyOnce(Action<ContainerBuilder> action){
        var context = new ContextCallOnlyOnce();
        
        Action<ContainerBuilder> ret = (builder)=>{
            if(false == context.AlreadyCalled){
                action(builder);
                context.AlreadyCalled = true;
            }
        };

        return ret;
    }

    class ContextCallOnlyOnce{
        public bool AlreadyCalled;
    }

    public AutofacConfigurationBuilder Callback(Action<ContainerBuilder> action)
    {
        action(_builder);
        return this;
    }

    public AutofacConfigurationBuilder RegisterSelf(Type program, bool inclueMediatr = true)
    {
        _builder.RegisterType(program).As(program).AsImplementedInterfaces().SingleInstance();

        if (inclueMediatr)
        {
            MediatrAssemblies.Add(program.Assembly);
        }

        return this;
    }
}

