using Amazon.SQS.Model;
using MediatR;
using Newtonsoft.Json;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor;

public class GameEventHandlers : 
    INotificationHandler<NewSessionStarted>, 
    INotificationHandler<RoundEnded>, 
    INotificationHandler<NewRoundStarted>,
    INotificationHandler<RoundExtended>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    private readonly IDelayProcessingService _delayProcessingService;

    public GameEventHandlers(IMediator mediator, 
        IDelayProcessingService delayProcessingService, 
        ILogger logger, 
        IClock clock)
    {
        _mediator = mediator;
        _logger = logger;
        _clock = clock;
        _delayProcessingService = delayProcessingService;
    }
    
    public Task Handle(NewSessionStarted x, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task Handle(RoundEnded detail, CancellationToken cancellationToken)
    {
        var q = await _mediator.Send(new GetSessionByIdQuery(detail.SessionId));
        if (q == null)
        {
            _logger.Log($"Could not load Session with id {detail.SessionId}.");
            return;
        }

        var session = q.Session;

        if (session.State != SessionState.ACTIVE)
        {
            _logger.Log($"Could not update Session with id {detail.SessionId}, it is not ACTIVE.");
            return;
        }

        var rounds = q.Rounds;
        var options = q.Options;

        var lastRound = rounds.FirstOrDefault(x => x.Id == detail.RoundId);
        if (lastRound == null)
        {
            _logger.Log(
                $"Could not find Round with id {detail.RoundId} in the rounds for Session {detail.SessionId}");
            return;
        }

        if (string.Equals(lastRound.Guess, session.Word, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.Log($"Correct answer found, ending Session {detail.SessionId} with SUCCESS");
            await _mediator.Send(new EndSessionCommand(detail.SessionId, session.Word, true, false));
            return;
        }
        else
        {
            if (rounds.Count < options.NumberOfRounds)
            {
                var roundId = await _mediator.Send(new CreateNewRoundCommand(detail.SessionId));
                _logger.Log($"Created new round {roundId} for Session {detail.SessionId}");
                return;
            }
            else
            {
                _logger.Log($"Incorrect final guess, ending Session {detail.SessionId}.");
                await _mediator.Send(new EndSessionCommand(detail.SessionId, session.Word, false, true));
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