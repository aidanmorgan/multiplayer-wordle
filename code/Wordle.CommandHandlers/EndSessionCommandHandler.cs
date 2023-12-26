using MediatR;
using Queries;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Model;
using Wordle.Persistence;

namespace Wordle.CommandHandlers;

public class EndSessionCommandHandler : IRequestHandler<EndSessionCommand, Unit>
{
    private readonly IClock _clock;
    private readonly IGameUnitOfWork _gameUnitOfWork;
    private readonly IMediator _mediator;


    public EndSessionCommandHandler(IClock clock, IGameUnitOfWork gameUnitOfWork, IMediator mediator)
    {
        _clock = clock;
        _gameUnitOfWork = gameUnitOfWork;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(EndSessionCommand request, CancellationToken cancellationToken)
    {
        if (!request.Fail && !request.Success)
        {
            throw new CommandException($"Cannot end Session with id {request.SessionId}, it is neither FAIL nor SUCCESS.");
        }

        SessionQueryResult? queryResult = await _mediator.Send(new GetSessionByIdQuery(request.SessionId));

        if (!queryResult.HasValue)
        {
            throw new CommandException($"Cannot find Session with id {request.SessionId}.");
        }

        Session session = queryResult.Value.Session;
        
        if (session == null)
        {
            throw new CommandException($"Cannot end Session with id {request.SessionId}, not found.");
        }

        if (session.State != SessionState.ACTIVE)
        {
            throw new CommandException($"Cannot end Session with id {request.SessionId}, the session is not {nameof(SessionState.ACTIVE)}.");
        }

        session.State = request.Success ? SessionState.SUCCESS : SessionState.FAIL;

        await _gameUnitOfWork.Sessions
            .UpdateAsync(session)
            .ContinueWith(x => _gameUnitOfWork.SaveAsync(), cancellationToken);

        if (request.Success)
        {
            await _mediator.Publish(new SessionEndedWithSuccess(session.Id), cancellationToken);
        }
        else
        {
            await _mediator.Publish(new SessionEndedWithFailure(session.Id), cancellationToken);
        }

        return Unit.Value;
    }
}