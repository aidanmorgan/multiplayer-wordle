using MediatR;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Queries;

namespace Wordle.CommandHandlers;

public class CreateNewRoundCommandHandler : IRequestHandler<CreateNewRoundCommand, Guid>
{
    private readonly IClock _clock;
    private readonly IMediator _mediator;
    private readonly IGameUnitOfWorkFactory _gameUnitOfWork;


    public CreateNewRoundCommandHandler(IClock clock, IMediator mediator, IGameUnitOfWorkFactory gameUnitOfWork)
    {
        _clock = clock;
        _mediator = mediator;
        _gameUnitOfWork = gameUnitOfWork;
    }

    public async Task<Guid> Handle(CreateNewRoundCommand request, CancellationToken cancellationToken)
    {
        var res = await _mediator.Send(new GetSessionByIdQuery(request.SessionId), cancellationToken);

        if (res == null)
        {
            throw new CommandException($"Cannot add a new round to Session {request.SessionId}, not found.");
        }

        var session = res.Session;

        if (session.State != SessionState.ACTIVE)
        {
            throw new CommandException($"Cannot start new round for Session {request.SessionId}, it's not active.");
        }

        if (session.ActiveRoundId.HasValue)
        {
            throw new CommandException(
                $"Cannot start new round for Session {request.SessionId}, already active round.");
        }
        
        var options = res.Options;
        var rounds = res.Rounds;

        if (rounds.Count >= options.NumberOfRounds)
        {
            throw new CommandException($"Cannot start new round for Session {request.SessionId}, too many rounds.");
        }

        var round = new Round()
        {
            Id = Ulid.NewUlid().ToGuid(),
            State = RoundState.ACTIVE,
            CreatedAt = _clock.UtcNow(),
            SessionId = session.Id
        };

        session.ActiveRoundEnd = _clock.UtcNow().Add(TimeSpan.FromSeconds(options.InitialRoundLength));
        session.ActiveRoundId = round.Id;

        var uow = _gameUnitOfWork.Create();
        await uow.Rounds.AddAsync(round);
        await uow.Sessions.UpdateAsync(session);

        await uow.SaveAsync();

        await _mediator.Publish(new NewRoundStarted(session.Tenant, session.Id, round.Id, session.ActiveRoundEnd.Value));

        return round.Id;
    }
}