using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wordle.Common;

namespace Wordle.Api.Common;

public class EventPublisherBackgroundService : IHostedService
{
    private readonly IEventPublisherService _publisherService;
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
    private readonly ILogger<EventPublisherBackgroundService> _logger;

    public EventPublisherBackgroundService(IEventPublisherService svx, ILogger<EventPublisherBackgroundService> logger)
    {
        _publisherService = svx;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {InterfaceName} ({ImplementationName})...", typeof(IEventPublisherService).Name, _publisherService.GetType().Name);
        await _publisherService.RunAsync(_cancellationToken.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {InterfaceName} ({ImplementationName})...", typeof(IEventPublisherService).Name, _publisherService.GetType().Name);
        await _cancellationToken.CancelAsync();
    }
}