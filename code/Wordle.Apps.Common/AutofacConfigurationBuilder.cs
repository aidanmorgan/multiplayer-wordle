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
using Wordle.Aws.Common;
using Wordle.Aws.DictionaryImpl;
using Wordle.Aws.EventBridge;
using Wordle.Aws.Kafka;
using Wordle.Aws.Kafka.Common;
using Wordle.Aws.Kafka.Consumer;
using Wordle.Aws.Kafka.Publisher;
using Wordle.Clock;
using Wordle.Dictionary;
using Wordle.Logger;
using Wordle.Persistence;
using Wordle.Persistence.Dynamo;
using Wordle.Render;

namespace Wordle.Apps.Common;

public class AutofacConfigurationBuilder
{
    private readonly ContainerBuilder _builder;
    
    private static readonly List<Assembly> MediatrAssemblies =
    [
        // we assume these are always required as we will be generating
        typeof(Wordle.CommandHandlers.Usings).Assembly,
        typeof(Wordle.QueryHandlers.Dynamo.Usings).Assembly
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
        b.RegisterInstance(new ConsoleLogger()).As<Wordle.Logger.ILogger>().SingleInstance();
        b.RegisterType<Clock.Clock>().As<IClock>().SingleInstance();

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
        EnvironmentVariables.EnvironmentFiles.ForEach(x =>
        {
            Console.WriteLine($"Found environment file {x}: {File.Exists(x)}");
        });
        
        _builder = i ?? new ContainerBuilder();
    }

    public void InitialiseDefaults()
    {
        InitialiseDefaultsCallback(_builder);
    }
    
    public AutofacConfigurationBuilder AddGamePersistence()
    {
        AddDynamoDbClient(_builder);
        
        _builder.RegisterType<DynamoGameConfiguration>()
            .As<DynamoGameConfiguration>()
            .WithParameter(new TypedParameter(typeof(string),
                EnvironmentVariables.GameDynamoTableName))
            .SingleInstance();

        _builder.RegisterInstance(new DynamoMappers()).As<IDynamoMappers>().SingleInstance();
        _builder.RegisterType<DynamoGameUnitOfWorkFactory>().As<IGameUnitOfWorkFactory>().SingleInstance();
        
        return this;
    }

    public AutofacConfigurationBuilder AddDictionary()
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
            InstanceId = instanceId
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

    public AutofacConfigurationBuilder RegisterSelf(Type program, bool inclueMediatr = true)
    {
        _builder.RegisterInstance(program).AsImplementedInterfaces().SingleInstance();

        if (inclueMediatr)
        {
            MediatrAssemblies.Add(program.Assembly);
        }

        return this;
    }
}

