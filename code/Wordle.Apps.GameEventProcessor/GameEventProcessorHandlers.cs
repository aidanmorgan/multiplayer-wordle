using Amazon.SQS.Model;
using Medallion.Threading;
using MediatR;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor;

public class GameEventProcessorHandlers : 
    INotificationHandler<RoundEnded>, 
    INotificationHandler<NewRoundStarted>
{
    public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    
    private readonly Guid _instance = Guid.NewGuid();
    private readonly IMediator _mediator;
    private readonly IDelayProcessingService _delayProcessingService;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<GameEventProcessorHandlers> _logger;

    public GameEventProcessorHandlers(IMediator mediator, 
        IDelayProcessingService delayProcessingService, 
        IDistributedLockProvider lockProvider,
        ILogger<GameEventProcessorHandlers> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _lockProvider = lockProvider;
        _delayProcessingService = delayProcessingService;
    }
    
    public async Task Handle(RoundEnded detail, CancellationToken cancellationToken)
    {
        await using var dLock = await _lockProvider.AcquireLockAsync($"Session:{detail.SessionId}", LockTimeout, cancellationToken);

        var q = await _mediator.Send(new GetSessionByIdQuery(detail.SessionId, detail.SessionVersion));
        if (q == null)
        {
            _logger.LogError("Could not load Session with id {SessionId}", detail.SessionId);
            return;
        }

        var session = q.Session;

        if (session.State != SessionState.ACTIVE)
        {
            _logger.LogError("Could not update Session with id {SessionId}, it is not ACTIVE", detail.SessionId);
            return;
        }

        var rounds = q.Rounds;
        var options = q.Options;

        var lastRound = rounds.FirstOrDefault(x => x.Id == detail.RoundId);
        if (lastRound == null)
        {
            _logger.LogError("Could not find Round with id {RoundId} in the rounds for Session {SessionId}",
                detail.RoundId, detail.SessionId);
            return;
        }

        if (string.Equals(lastRound.Guess, session.Word, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogInformation("Correct answer found, ending Session {SessionId} with SUCCESS",
                detail.SessionId);
            await _mediator.Send(new EndSessionCommand(detail.SessionId, detail.SessionVersion, session.Word, true,
                false));
            return;
        }
        else
        {
            try
            {
                if (rounds.Count < options.NumberOfRounds)
                {
                    var roundId =
                        await _mediator.Send(new CreateNewRoundCommand(detail.SessionId, detail.SessionVersion));
                    _logger.LogInformation("Created new round {RoundId} for Session {SessionId}", roundId,
                        detail.SessionId);
                    return;
                }
                else
                {
                    _logger.LogInformation("Incorrect final guess, ending Session {SessionId}", detail.SessionId);
                    await _mediator.Send(new EndSessionCommand(detail.SessionId, detail.SessionVersion,
                        session.Word, false, true));
                }
            }
            catch (CommandException x)
            {
                _logger.LogError(x, "Error update state of Session {DetailSessionId}", detail.SessionId);
            }
        }
    }

    public async Task Handle(NewRoundStarted n, CancellationToken cancellationToken)
    {
        await using var dLock = await _lockProvider.AcquireLockAsync($"Session:{n.SessionId}", LockTimeout, cancellationToken);

        _logger.LogInformation($"NewRoundStarted - {n.Id}, Handler - {_instance}");

        var session = new VersionId()
        {
            Id = n.SessionId,
            Version = n.SessionVersion
        };

        var round = new VersionId()
        {
            Id = n.RoundId,
            Version = n.RoundVersion
        };
        
        await _delayProcessingService.ScheduleRoundUpdate(session, round, n.RoundExpiry, cancellationToken);
    }

    /*
    // this isn't needed as we moved the round extension logic into the end round handler, that means that we no longer
    // have to listen for this event as when it is generated this service will already have it enqueued
    public async Task Handle(RoundExtended n, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"RoundExtended - {n.Id}, Handler - {_instance}");
        await _delayProcessingService.ScheduleRoundUpdate(n.SessionId, n.RoundId, n.RoundExpiry, cancellationToken);
    }
    */
}