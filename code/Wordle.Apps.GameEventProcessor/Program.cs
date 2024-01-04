// See https://aka.ms/new-console-template for more information

using Autofac;
using Microsoft.Extensions.Logging;
using Wordle.Apps.Common;
using Wordle.Apps.GameEventProcessor.Impl;
using Wordle.Common;

namespace Wordle.Apps.GameEventProcessor;

public class Program 
{
    private static readonly TimeSpan MaxCleanShutdownWait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumStartTimeout = TimeSpan.FromSeconds(10);
    
    
    private readonly ILogger<Program> _logger;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly IEventConsumerService _eventConsumerService;
    private readonly IDelayProcessingService _delayProcessingService;
    private readonly CancellationTokenSource _systenShutdown;

    public static void Main()
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.GetName().Name, "d66c2093-964e-4f94-9a24-49e7b6cabfd2");
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddPostgresPersistence()
            .AddDynamoDictionary()
            .AddActiveMqEventPublisher(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddActiveMqEventConsumer(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);

        configBuilder.RegisterSelf(typeof(Program));
        configBuilder.Callback(x =>
        {
            x.RegisterType<GameEventProcessorHandlers>()
                .SingleInstance()
                .As<GameEventProcessorHandlers>();

            x.RegisterInstance(new ActiveMqDelayProcessingOptions()
            {
                ActiveMqUri = EnvironmentVariables.ActiveMqBrokerUrl,
                InstanceType = EnvironmentVariables.InstanceType,
                InstanceId = EnvironmentVariables.InstanceId
            })
            .As<ActiveMqDelayProcessingOptions>()
            .SingleInstance();

            x.RegisterType<ActiveMqDelayProcessingService>()
                .As<IDelayProcessingService>()
                .SingleInstance();
        });
        
        var container = configBuilder.Build();
        var program = container.Resolve<Program>(); 
        
        program.Run();
    }
    
    public Program(ILogger<Program> logger, IEventPublisherService eps, IEventConsumerService ecs, IDelayProcessingService dps)
    {
        _logger = logger;

        _eventPublisherService = eps;
        _eventConsumerService = ecs;
        _delayProcessingService = dps;

        _systenShutdown = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => _systenShutdown.Cancel(); 
    }

    private void Run()
    {
        var backgroundTasks = new Dictionary<string,Task>() { 
            {nameof(IEventConsumerService), Task.Run(async () => await _eventConsumerService.RunAsync(_systenShutdown.Token)) }, 
            {nameof(IEventPublisherService), Task.Run(async () => await _eventPublisherService.RunAsync(_systenShutdown.Token))}, 
            {nameof(IDelayProcessingService), Task.Run(async () => await _delayProcessingService.RunAsync(_systenShutdown.Token)) } 
        };
        
        Task.WaitAny(backgroundTasks.Values.ToArray());

        // if we get here and the cancellation token hasn't been requested it means one of the background threads
        // has died, which means we now want to kill the remaining threads so we can try and shut down cleanly.
        if (!_systenShutdown.IsCancellationRequested)
        {
            _logger.LogInformation("Attempting clean shutdown. Will take {TotalSeconds} seconds", MaxCleanShutdownWait.TotalSeconds);
            _systenShutdown.Cancel();
            Task.WaitAll(backgroundTasks
                .Where(x => !x.Value.IsFaulted)
                .Select(x => x.Value)
                .ToArray(), MaxCleanShutdownWait);
        }


        if (backgroundTasks.Any(x => x.Value.IsFaulted))
        {
            backgroundTasks
                .Where(x => x.Value.IsFaulted)
                .ToList()
                .ForEach(x =>
                {
                    _logger.LogCritical(x.Value.Exception, "Background service {ServiceName} failed, SHUTTING DOWN", x.Key);
                });
        }
        else
        {
            _logger.LogInformation("Exiting cleanly...");
        }
    }
}