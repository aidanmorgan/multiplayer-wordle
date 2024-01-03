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
                .AsImplementedInterfaces();

            x.RegisterInstance(new ActiveMqDelayProcessingSettings()
            {
                InstanceType = EnvironmentVariables.InstanceType,
                InstanceId = EnvironmentVariables.InstanceId
            })
            .As<ActiveMqDelayProcessingSettings>()
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
        var eventPublisher = _eventPublisherService.RunAsync(_systenShutdown.Token);
        var eventConsumer = _eventConsumerService.RunAsync(_systenShutdown.Token);
        var delayProcessing = _delayProcessingService.RunAsync(_systenShutdown.Token);
        
        var all = new Task[] { eventPublisher, eventConsumer, delayProcessing };
        
        Task.WaitAny(all);

        // if we get here and the cancellation token hasn't been requested it means one of the background threads
        // has died, which means we now want to kill the remaining threads so we can try and shut down cleanly.
        if (!_systenShutdown.IsCancellationRequested)
        {
            _logger.LogInformation("Attempting clean shutdown. Will take {TotalSeconds} seconds", MaxCleanShutdownWait.TotalSeconds);
            _systenShutdown.Cancel();
            Task.WaitAll(all.Where(x => !x.IsFaulted).ToArray(), MaxCleanShutdownWait);
        }

        if (eventPublisher.IsFaulted)
        {
            _logger.LogCritical(eventPublisher.Exception, "Event Publisher failed");            
        } 
        
        if (eventConsumer.IsFaulted)
        {
            _logger.LogCritical(eventConsumer.Exception, "Event Consumer failed");            
        }

        if (delayProcessing.IsFaulted)
        {
            _logger.LogCritical(delayProcessing.Exception, "Delay Processor failed");
        }

        if (!eventConsumer.IsFaulted && !delayProcessing.IsFaulted && !eventPublisher.IsFaulted)
        {
            _logger.LogInformation("Exiting cleanly...");
        }
    }
}