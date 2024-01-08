using Medallion.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class InitialisationService : IInitialisationService
{
    private readonly GameEventProcessorOptions _options;
    private readonly IMediator _mediator;
    private readonly IDelayProcessingService _delayProcessingService;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<InitialisationService> _logger;

    public InitialisationService(IMediator mediator, IDelayProcessingService svc, IDistributedLockProvider lockProvider, GameEventProcessorOptions options, ILogger<InitialisationService> logger)
    {
        _mediator = mediator;
        _delayProcessingService = svc;
        _lockProvider = lockProvider;
        _options = options;
        _logger = logger;
    }
    
    public async Task<bool> RunAsync(CancellationToken token)
    {
        var activeSessions = await _mediator.Send(new GetAllActiveSessionsQuery(), token);

        foreach (var pair in activeSessions)
        {
            var dLockKey = _options.SessionLockKey(pair.SessionId);
            await using (var dLock = await _lockProvider.TryAcquireLockAsync(dLockKey, _options.DistributedLockTimeout, cancellationToken: token))
            {
                if (dLock == null)
                {
                    _logger.LogWarning("Failed to obtain lock {LockKey}, aborting", dLockKey);
                }

                await _delayProcessingService.ScheduleRoundUpdate(pair.VersionedSession, pair.VersionedRound,
                    pair.RoundExpiry, token);
            }
        }

        return !token.IsCancellationRequested;
    }
}