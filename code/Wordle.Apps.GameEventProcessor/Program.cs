// See https://aka.ms/new-console-template for more information

using Autofac;
using Medallion.Threading;
using Medallion.Threading.Postgres;
using Microsoft.Extensions.Logging;
using Wordle.Api.Common;
using Wordle.Apps.Common;
using Wordle.Apps.GameEventProcessor.Impl;
using Wordle.Common;

namespace Wordle.Apps.GameEventProcessor;

public class Program 
{
    
    
    private readonly ILogger<Program> _logger;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly IEventConsumerService _eventConsumerService;
    private readonly IDelayProcessingService _delayProcessingService;
    private readonly CancellationTokenSource _systenShutdown;
    private readonly IInitialisationService _initialisationService;
    private readonly IPeriodicCleanupService _periodicCleanupService;
    private readonly GameEventProcessorOptions _options;

    public static async Task Main()
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.GetName().Name, "d66c2093-964e-4f94-9a24-49e7b6cabfd2");
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddPostgresPersistence(false)
            .AddActiveMqEventPublisher(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddActiveMqEventConsumer(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);

        configBuilder.RegisterSelf(typeof(Program));
        configBuilder.Callback(x =>
        {
            x.RegisterInstance(new GameEventProcessorOptions()
                {
                    ActiveMqUri = EnvironmentVariables.ActiveMqBrokerUrl,
                    InstanceType = EnvironmentVariables.InstanceType,
                    InstanceId = EnvironmentVariables.InstanceId
                })
                .SingleInstance();
            
            x.RegisterType<InitialisationService>()
                .As<IInitialisationService>()
                .SingleInstance();

            x.RegisterType<PostgresPeriodicCleanupService>()
                .As<IPeriodicCleanupService>()
                .SingleInstance();
            
            x.RegisterType<GameEventProcessorHandlers>()
                .SingleInstance()
                .As<GameEventProcessorHandlers>();
            
            x.RegisterType<ActiveMqDelayProcessingService>()
                .As<IDelayProcessingService>()
                .SingleInstance();

            x.RegisterType<PostgresDistributedSynchronizationProvider>()
                .As<IDistributedLockProvider>()
                .WithParameter(new PositionalParameter(0, EnvironmentVariables.PostgresConnectionString))
                .SingleInstance();
        });
        
        var container = configBuilder.Build();
        var program = container.Resolve<Program>(); 
        
        await program.Run();
    }
    
    public Program(GameEventProcessorOptions options, ILogger<Program> logger, IInitialisationService ins, IEventPublisherService eps, IEventConsumerService ecs, IDelayProcessingService dps, IPeriodicCleanupService pcs)
    {
        _logger = logger;

        _options = options;
        _initialisationService = ins;
        _eventPublisherService = eps;
        _eventConsumerService = ecs;
        _delayProcessingService = dps;
        _periodicCleanupService = pcs;

        _systenShutdown = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => _systenShutdown.Cancel(); 
    }

    private async Task Run()
    {
        _logger.LogInformation("Starting {Name} with type: {Type} and id: {Id}", typeof(Program).Assembly.GetName(), EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);

        // start all the background tasks
        var backgroundTasks = new Dictionary<string,Task>() { 
            {nameof(IEventConsumerService), Task.Run(async () => await _eventConsumerService.RunAsync(_systenShutdown.Token)) }, 
            {nameof(IEventPublisherService), Task.Run(async () => await _eventPublisherService.RunAsync(_systenShutdown.Token))}, 
            {nameof(IDelayProcessingService), Task.Run(async () => await _delayProcessingService.RunAsync(_systenShutdown.Token)) },
            {nameof(IPeriodicCleanupService), Task.Run(async () => await _periodicCleanupService.RunAsync(_systenShutdown.Token)) }
        };
        
        // do an initialisation wait check
        var initialised = await Task.WhenAll(
            Task.Run( () => _eventPublisherService.ReadySignal.Wait(_options.MaximumStartTimeout)),
            Task.Run( () => _eventConsumerService.ReadySignal.Wait(_options.MaximumStartTimeout)),
            Task.Run( () => _delayProcessingService.ReadySignal.Wait(_options.MaximumStartTimeout)),
            
            // this is run once and then exits, so we should do it this loop rather that in the background tasks group
            Task.Run(() => _initialisationService.RunAsync(_systenShutdown.Token))
        );

        if (!initialised.All(x => x))
        {
            _logger.LogCritical("Initialisation of background threads took too long, exiting.");
            await _systenShutdown.CancelAsync();
        }
        else
        {
            // all background services have initialised inside the startup time, so we're now going to block processing
            // of the main thread until one of the background services returns with an issue or the system shutdown kicks in
            await Task.WhenAny(backgroundTasks.Values.ToArray());
        }

        if (!_systenShutdown.IsCancellationRequested)
        {
            _logger.LogInformation("Attempting clean shutdown. Will take {TotalSeconds} seconds", _options.MaxCleanShutdownWait.TotalSeconds);
            await _systenShutdown.CancelAsync();
            
            
            Task.WaitAll(backgroundTasks
                .Where(x => !x.Value.IsFaulted)
                .Select(x => x.Value)
                .ToArray(), _options.MaxCleanShutdownWait);
        }
        
        // now check to see if any of the background tasks have returned a fault state, we're probably interested in that fact
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