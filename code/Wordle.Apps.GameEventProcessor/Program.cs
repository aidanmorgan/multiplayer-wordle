// See https://aka.ms/new-console-template for more information

using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using Hangfire;
using Hangfire.Redis.StackExchange;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using Wordle.Apps.Common;
using Wordle.Apps.GameEventProcessor.Impl;
using Wordle.Aws.Common;
using Wordle.Aws.EventBridge;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Queries;
using IContainer = Autofac.IContainer;

namespace Wordle.Apps.GameEventProcessor;

public class Program 
{
    private readonly ILogger _logger;
    private readonly IEventConsumerService _eventConsumerService;
    private readonly IDelayProcessingService _delayProcessingService;

    public static void Main()
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.FullName, "d66c2093-964e-4f94-9a24-49e7b6cabfd2");
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddDictionary()
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
    
    public Program(ILogger logger, IEventConsumerService ecs, IDelayProcessingService dps)
    {
        _logger = logger;
        
        _eventConsumerService = ecs;
        _delayProcessingService = dps;
    }

    private void Run()
    {
        var token = new CancellationTokenSource();
        
        Task.WaitAny(
            _eventConsumerService.RunAsync(token.Token), 
            _delayProcessingService.RunAsync(token.Token)
        );
        
        _logger.Log("Exiting.");
    }
}