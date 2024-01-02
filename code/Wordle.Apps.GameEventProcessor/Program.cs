// See https://aka.ms/new-console-template for more information

using Autofac;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wordle.Apps.Common;
using Wordle.Apps.GameEventProcessor.Impl;
using Wordle.Aws.Common;

namespace Wordle.Apps.GameEventProcessor;

public class Program 
{
    private readonly ILogger<Program> _logger;
    private readonly IEventConsumerService _eventConsumerService;
    private readonly IDelayProcessingService _delayProcessingService;

    public static void Main()
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.FullName, "d66c2093-964e-4f94-9a24-49e7b6cabfd2");
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddPostgresPersistence()
            .AddDynamoDictionary()
            .AddRedisEventPublisher(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddRedisEventConsumer(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);

        configBuilder.RegisterSelf(typeof(Program));
        configBuilder.Callback(x =>
        {
            x.RegisterType<GameEventHandlers>().SingleInstance().AsImplementedInterfaces();
        });
        
        // if using hangfire
        configBuilder.Callback(x =>
        {
            x.RegisterType<HangfireDelayProcessingService>()
                .As<IDelayProcessingService>()
                .SingleInstance();
        });
        
/*        configBuilder.Callback(x =>
        {
            x.RegisterType<SqsEventConsumerService>()
                .As<IDelayProcessingService>()
                .SingleInstance();
        });
*/        
        var container = configBuilder.Build();
        
        GlobalConfiguration.Configuration
            .UseRedisStorage(ConnectionMultiplexer.Connect(EnvironmentVariables.RedisServer))
            .UseAutofacActivator(container.BeginLifetimeScope("hangfire-scope"));

        var program = container.Resolve<Program>(); 
        program.Run();
    }
    
    public Program(ILogger<Program> logger, IEventConsumerService ecs, IDelayProcessingService dps)
    {
        _logger = logger;
        
        _eventConsumerService = ecs;
        _delayProcessingService = dps;
    }

    private void Run()
    {
        var token = new CancellationTokenSource();

        var eventConsumer = _eventConsumerService.RunAsync(token.Token);
        var delayProcessing = _delayProcessingService.RunAsync(token.Token);
        
        Task.WaitAny(
            eventConsumer,
            delayProcessing
        );
        
        if (eventConsumer.IsFaulted)
        {
            _logger.LogCritical(eventConsumer.Exception, "Event Consumer failed");            
        }

        if (delayProcessing.IsFaulted)
        {
            _logger.LogCritical(delayProcessing.Exception, "Delay Processor failed");
        }

        if (!eventConsumer.IsFaulted && !delayProcessing.IsFaulted)
        {
            _logger.LogInformation("Exiting...");
        }
    }
}