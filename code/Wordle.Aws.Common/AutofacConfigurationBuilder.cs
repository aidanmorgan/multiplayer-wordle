using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.S3;
using Amazon.SQS;
using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;
using Wordle.Aws.DictionaryImpl;
using Wordle.Aws.EventBridgeImpl;
using Wordle.Clock;
using Wordle.Dictionary;
using Wordle.Logger;
using Wordle.Persistence;
using Wordle.Persistence.Dynamo;

namespace Wordle.Aws.Common;

public class AutofacConfigurationBuilder
{
    private ContainerBuilder builder;

    private readonly Action<ContainerBuilder> AddDynamoDbClient = callOnlyOnce((b) =>
    { 
        b.RegisterInstance(new AmazonDynamoDBClient()).As<IAmazonDynamoDB>().SingleInstance();
    });

    private readonly Action<ContainerBuilder> AddS3Client = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonS3Client()).As<IAmazonS3>().SingleInstance();
    });

    private readonly Action<ContainerBuilder> AddEventBridgeClient = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonEventBridgeClient()).As<IAmazonEventBridge>().SingleInstance();
    });

    private readonly Action<ContainerBuilder> AddSqsClient = callOnlyOnce((b) =>
    {
        b.RegisterInstance(new AmazonSQSClient()).As<IAmazonSQS>().SingleInstance();
    });


    public AutofacConfigurationBuilder()
    {
        builder = new ContainerBuilder();
        builder.RegisterInstance(new ConsoleLogger()).As<Wordle.Logger.ILogger>().SingleInstance();
    }
    
    public AutofacConfigurationBuilder AddGamePersistence()
    {
        AddDynamoDbClient(builder);
        
        builder.RegisterType<DynamoGameConfiguration>()
            .As<DynamoGameConfiguration>()
            .WithParameter(new TypedParameter(typeof(string),
                EnvironmentVariables.GameDynamoTableName))
            .SingleInstance();
            
        builder.RegisterType<DynamoGameUnitOfWork>().As<IGameUnitOfWork>()
            .InstancePerLifetimeScope();
        builder.RegisterInstance(new DynamoMappers()).As<IDynamoMappers>();
        
        return this;
    }

    public AutofacConfigurationBuilder AddDictionary()
    {
        AddDynamoDbClient(builder);

        builder.RegisterType<DynamoDictionaryConfiguration>()
            .As<DynamoDictionaryConfiguration>()
            .WithParameter(new TypedParameter(typeof(string),
                EnvironmentVariables.DictionaryDynamoTableName))
            .SingleInstance();

        builder.RegisterType<DynamoDbWordleDictionaryService>()
            .As<IWordleDictionaryService>()
            .SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddImagePersistence()
    {
        AddS3Client(builder);

        return this;
    }

    public AutofacConfigurationBuilder AddEventBridge()
    {
        AddEventBridgeClient(builder);
        
        builder.RegisterType<EventBridgePublisher>()
            // have to do this because we implement a bunch of INotificationHandler(s) that need to be notified by MediatR
            .AsImplementedInterfaces()
            .WithParameter(new TypedParameter(typeof(string),
                EnvironmentVariables.EventBridgeName))
            .SingleInstance();

        return this;
    }

    public AutofacConfigurationBuilder AddEventHandling()
    {
        AddSqsClient(builder);
        return this;
    }

    public IContainer Build()
    {
        builder.RegisterType<Clock.Clock>().As<IClock>().SingleInstance();

        // add all of the mediatr stuff, as it's pretty much always used
        var configuration = MediatRConfigurationBuilder
            .Create(
                typeof(Wordle.CommandHandlers.Usings).Assembly, 
                typeof(Wordle.QueryHandlers.Dynamo.Usings).Assembly
            )
            .WithAllOpenGenericHandlerTypesRegistered()
            .Build();

        
        builder.RegisterMediatR(configuration);

        
        return builder.Build();
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

}

