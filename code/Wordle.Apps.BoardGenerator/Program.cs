// See https://aka.ms/new-console-template for more information

using Autofac;
using Microsoft.Extensions.Logging;
using Wordle.Apps.BoardGenerator.Impl;
using Wordle.Apps.Common;
using Wordle.Common;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class Program
{
    private static readonly TimeSpan MaxInitialisationWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxCleanShutdownWait = TimeSpan.FromSeconds(10);

    private readonly IEventConsumerService _eventConsumerService;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly ILogger<Program> _logger;
    private CancellationTokenSource _systenShutdown;

    public static async Task Main()
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.GetName().Name, "9a81b0d6-e62b-4e11-b7a7-7040095de6f8");
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddPostgresPersistence()
            .AddActiveMqEventPublisher(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddActiveMqEventConsumer(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddRenderer()
            .RegisterSelf(typeof(Program));

        configBuilder.Callback(x =>
        {
            x.RegisterType<BoardGeneratorHandlers>().As<BoardGeneratorHandlers>();
            x.RegisterType<LocalDiskStorage>()
                .As<IBoardStorage>()
                .WithParameter(new PositionalParameter(0, EnvironmentVariables.ImagesDirectory))
                .SingleInstance();
        });

        var container = configBuilder.Build();

        var program = container.Resolve<Program>(); 
        await program.Run();
    }
    
    public Program(IEventPublisherService eps, IEventConsumerService ecs, ILogger<Program> logger)
    {
        _eventPublisherService = eps;
        _eventConsumerService = ecs;
        _logger = logger;
        
        _logger.LogInformation("Starting {Name} with type: {Type} and id: {Id}", typeof(Program).Assembly.GetName(), EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);
        
        _systenShutdown = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => _systenShutdown.Cancel(); 

    }

    private async Task Run()
    {
        var backgroundTasks = new Dictionary<string,Task>() { 
            {nameof(IEventConsumerService), Task.Run(async () => await _eventConsumerService.RunAsync(_systenShutdown.Token), _systenShutdown.Token) }, 
            {nameof(IEventPublisherService), Task.Run(async () => await _eventPublisherService.RunAsync(_systenShutdown.Token), _systenShutdown.Token)}, 
        };

        var initialisations = await Task.WhenAll(
            Task.Run(() => _eventPublisherService.ReadySignal.Wait(MaxInitialisationWait)),
            Task.Run(() => _eventConsumerService.ReadySignal.Wait(MaxInitialisationWait))
        );

        if (initialisations.Any(x => !x))
        {
            _logger.LogCritical( $"Initialisation of background threads took too long, exiting.");
            _systenShutdown.Cancel();

        }

        await Task.WhenAny(backgroundTasks.Select(x => x.Value).ToArray());

        // if we get here and the cancellation token hasn't been requested it means one of the background threads
        // has died, which means we now want to kill the remaining threads so we can try and shut down cleanly.
        if (!_systenShutdown.IsCancellationRequested)
        {
            _logger.LogInformation("Attempting clean shutdown. Will take {TotalSeconds} seconds", MaxCleanShutdownWait.TotalSeconds);
            _systenShutdown.Cancel();
            Task.WaitAll(backgroundTasks
                .Select(x => x.Value)
                .Where(x => !x.IsFaulted).ToArray(), MaxCleanShutdownWait);
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