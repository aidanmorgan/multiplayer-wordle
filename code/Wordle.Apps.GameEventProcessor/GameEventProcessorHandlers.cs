using Amazon.SQS.Model;
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
    INotificationHandler<NewRoundStarted>,
    INotificationHandler<RoundExtended>
{
    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly IDelayProcessingService _delayProcessingService;
    private readonly ILogger<GameEventProcessorHandlers> _logger;

    public GameEventProcessorHandlers(IMediator mediator, 
        IDelayProcessingService delayProcessingService, 
        IClock clock, ILogger<GameEventProcessorHandlers> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _clock = clock;
        _delayProcessingService = delayProcessingService;
    }
    
    public async Task Handle(RoundEnded detail, CancellationToken cancellationToken)
    {
        var q = await _mediator.Send(new GetSessionByIdQuery(detail.SessionId));
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
            _logger.LogError("Could not find Round with id {RoundId} in the rounds for Session {SessionId}", detail.RoundId, detail.SessionId);
            return;
        }

        if (string.Equals(lastRound.Guess, session.Word, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogInformation("Correct answer found, ending Session {SessionId} with SUCCESS", detail.SessionId);
            await _mediator.Send(new EndSessionCommand(detail.SessionId, session.Word, true, false));
            return;
        }
        else
        {
            try
            {
                if (rounds.Count < options.NumberOfRounds)
                {
                    var roundId = await _mediator.Send(new CreateNewRoundCommand(detail.SessionId));
                    _logger.LogInformation("Created new round {RoundId} for Session {SessionId}", roundId,
                        detail.SessionId);
                    return;
                }
                else
                {
                    _logger.LogInformation("Incorrect final guess, ending Session {SessionId}", detail.SessionId);
                    await _mediator.Send(new EndSessionCommand(detail.SessionId, session.Word, false, true));
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
        await _delayProcessingService.ScheduleRoundUpdate(n.SessionId, n.RoundId, n.RoundExpiry, cancellationToken);
    }

    public async Task Handle(RoundExtended n, CancellationToken cancellationToken)
    {
        await _delayProcessingService.ScheduleRoundUpdate(n.SessionId, n.RoundId, n.RoundExpiry, cancellationToken);
    }
}