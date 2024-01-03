// See https://aka.ms/new-console-template for more information

using Amazon.S3;
using Amazon.SQS;
using Autofac;
using MediatR;
using Microsoft.Extensions.Logging;
using Wordle.Apps.BoardGenerator.Impl;
using Wordle.Apps.Common;
using Wordle.Common;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class Program 
{
    private static readonly TimeSpan MaxCleanShutdownWait = TimeSpan.FromSeconds(5);

    private readonly IEventConsumerService _eventConsumerService;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly ILogger<Program> _logger;
    private CancellationTokenSource _systenShutdown;

    public static void Main()
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
        program.Run();
    }
    
    public Program(IEventPublisherService eps, IEventConsumerService ecs, ILogger<Program> logger)
    {
        _eventPublisherService = eps;
        _eventConsumerService = ecs;
        _logger = logger;
        
        _systenShutdown = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => _systenShutdown.Cancel(); 

    }

    private void Run()
    {
        var eps = _eventPublisherService.RunAsync(_systenShutdown.Token);
        var ecs = _eventConsumerService.RunAsync(_systenShutdown.Token);

        var all = new Task[] { eps, ecs };
        
        Task.WaitAny(all);

        // if we get here and the cancellation token hasn't been requested it means one of the background threads
        // has died, which means we now want to kill the remaining threads so we can try and shut down cleanly.
        if (!_systenShutdown.IsCancellationRequested)
        {
            _logger.LogInformation("Attempting clean shutdown. Will take {TotalSeconds} seconds", MaxCleanShutdownWait.TotalSeconds);
            _systenShutdown.Cancel();
            Task.WaitAll(all.Where(x => !x.IsFaulted).ToArray(), MaxCleanShutdownWait);
        }
        
        if (eps.IsFaulted)
        {
            _logger.LogCritical(eps.Exception, "Exiting...");
        }

        if (ecs.IsFaulted)
        {
            _logger.LogCritical(ecs.Exception, "Exiting...");
        }
        
        if(!eps.IsFaulted && !ecs.IsFaulted)
        {
            _logger.LogInformation("Exiting cleanly....");
        }
    }
}