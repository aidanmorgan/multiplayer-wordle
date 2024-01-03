
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wordle.Common;

namespace Wordle.Api.Common;

public class EventConsumerBackgroundService : IHostedService
{
    private readonly IEventConsumerService _consumerService;
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
    private readonly ILogger<EventConsumerBackgroundService> _logger;

    public EventConsumerBackgroundService(IEventConsumerService svx, ILogger<EventConsumerBackgroundService> logger)
    {
        _consumerService = svx;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {InterfaceName} ({ImplementationName})...", typeof(IEventConsumerService).Name, _consumerService.GetType().Name);
        await _consumerService.RunAsync(_cancellationToken.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {InterfaceName} ({ImplementationName})...", typeof(IEventConsumerService).Name, _consumerService.GetType().Name);
        await _cancellationToken.CancelAsync();
    }
}