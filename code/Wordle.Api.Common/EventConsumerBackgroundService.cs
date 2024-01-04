
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wordle.Common;

namespace Wordle.Api.Common;

public class EventConsumerBackgroundService : IHostedService
{
    private readonly IEventConsumerService _consumerService;
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
    private readonly ILogger<EventConsumerBackgroundService> _logger;
    private Task _task;

    public EventConsumerBackgroundService(IEventConsumerService svx, ILogger<EventConsumerBackgroundService> logger)
    {
        _consumerService = svx;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {InterfaceName} ({ImplementationName})...", typeof(IEventConsumerService).Name, _consumerService.GetType().Name);
        _task = Task.Run(async () => await _consumerService.RunAsync(_cancellationToken.Token));
          
        _consumerService.ReadySignal.Wait(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {InterfaceName} ({ImplementationName})...", typeof(IEventConsumerService).Name, _consumerService.GetType().Name);
        await _cancellationToken.CancelAsync();
    }
}