using MediatR;
using Wordle.Apps.Common;
using Wordle.Commands;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor.Impl;

public abstract class AbstractDelayProcessingService : IDelayProcessingService
{
    protected readonly IMediator Mediator;
    protected readonly ILogger Logger;

    public AbstractDelayProcessingService(IMediator mediator, ILogger logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    protected async Task HandleTimeout(TimeoutPayload p)
    {
        var qr = await Mediator.Send(new GetSessionByIdQuery(p.SessionId));

        if (qr == null)
        {
            Logger.Log($"Cannot end active round for session {p.SessionId}, no Session found.");
            return;
        }

        var session = qr.Session;

        if (session.State != SessionState.ACTIVE)
        {
            Logger.Log($"Cannot end active round for Session {session.Id}, it is not ACTIVE.");
            return;
        }

        if (!session.ActiveRoundId.HasValue)
        {
            Logger.Log($"Cannot end active round for Session {session.Id}, there is no active round.");
            return;
        }

        if (!session.ActiveRoundEnd.HasValue)
        {
            Logger.Log($"Cannot end active round for Session {session.Id}. there is no active round end set.");
            return;
        }

        if (!p.RoundId.Equals(session.ActiveRoundId.Value) )
        {
            Logger.Log($"Cannot end Round {p.RoundId} for Session {session.Id}. it is not the active round.");
            return;
        }

        try
        {
            await Mediator.Send(new EndActiveRoundCommand(session.Id));
        }
        catch (CommandException)
        {
            // ignore
        }
        
    }

    public abstract Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken token);
    public abstract Task RunAsync(CancellationToken token);
}