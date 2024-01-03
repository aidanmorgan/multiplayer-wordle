using MediatR;
using Microsoft.VisualBasic;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Queries;

namespace Wordle.CommandHandlers;

public class AddGuessToRoundCommandHandler : IRequestHandler<AddGuessToRoundCommand, Unit>
{
    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly IGameUnitOfWorkFactory _uowFactory;


    public AddGuessToRoundCommandHandler(IMediator mediator, IClock clock, IGameUnitOfWorkFactory factory)
    {
        _mediator = mediator;
        _clock = clock;
        _uowFactory = factory;
    }

    public async Task<Unit> Handle(AddGuessToRoundCommand request, CancellationToken cancellationToken)
    {
        SessionQueryResult? queryResult = await _mediator.Send(new GetSessionByIdQuery(request.SessionId));
        if (queryResult == null)
        {
            throw new CommandException($"Cannot load Session with Id {request.SessionId}.");
        }

        Session session = queryResult.Session;
        if (session.State != SessionState.ACTIVE)
        {
            throw new CommandException($"Cannot add Guess to Round for Session {request.SessionId}, it is in an invalid state.");
        }

        Round? round = queryResult.Rounds.FirstOrDefault(x => x.Id == session.ActiveRoundId)!;
        Options options = queryResult.Options;

        if (round == null)
        {
            throw new CommandException(
                $"Cannot add Guess to Round for Session {request.SessionId}, the active round is invalid.");
        }

        if (round.State == RoundState.INACTIVE)
        {
            if (!options.AllowGuessesAfterRoundEnd && (_clock.UtcNow().IsAfter(request.Timestamp)))
            {
                throw new CommandException(
                    $"Cannot add Guess to Round for Session {request.SessionId}, the round has expired.");
            }
        }

        // BUG:
        // There used to be a check here to make sure that the guess timestamp was before the round end time, but
        // it creates an interesting race condition where the round end is in the process of being updated.
        // Rather than reject the users input (which can feel kind of gross) we will accept the input and store
        // it, however if a round end is already being determined then this guess is effectively throwaway.
        //
        // The are corner cases here  still - if this guess would have created a tie then it's possible that
        // we have chosen the wrong word. Also if there was already a tie then this guess could have tipped the word
        // selection over to a different word. AFAIK this isn't an auditable financial system, so we'll just have to
        // monitor (TODO: write query to detect this scenario).
        Guid guessId = Ulid.NewUlid().ToGuid();

        var uow = _uowFactory.Create();
        await uow.Guesses.AddAsync(new Guess()
        {
            Id = guessId,
            Timestamp = request.Timestamp,
            Word = request.Word.ToUpperInvariant(),
            RoundId = round.Id,
            SessionId = session.Id,
            User = request.User
        });

//        bool roundEndUpdated = await UpdateRoundEndForNewGuess(uow, session, options, round);
        
        await uow.SaveAsync();
        // broadcast this first before the guess add as it's more important to get out.
//        if (roundEndUpdated)
//        {
//            await _mediator.Publish(new RoundExtended(session.Tenant, session.Id, round.Id, session.ActiveRoundEnd!.Value), cancellationToken);
//        }
        
        await _mediator.Publish(new GuessAdded(session.Tenant, guessId, round.Id, session.Id), cancellationToken);
        
        
        return Unit.Value;
    }

    private async Task<bool> UpdateRoundEndForNewGuess(IGameUnitOfWork uow, Session session, Options options, Round round)
    {
        if (session.ActiveRoundEnd == null)
        {
            throw new CommandException($"Cannot update round end, there is no {nameof(session.ActiveRoundEnd)} set.");
        }

        // if the guess has come in, but we are within the extension window of the round, then we need to update
        // the round end time - this is a weird design quirk of playing multiplayer wordle.
        //
        // The scenario this is attempting to capture is - if there are no guesses, and then a user submits a guess
        // 1 second before the end of the round then it's possible that the guess is just selected automatically
        // by the round-end processor. Whilst this isnt the worst thing in the world, it's kinda annoying
        // so what we do is just extend the round out by the @{see Options.RoundExtensionLength} amount to allow
        // more guesses to come in OR for people to vote as appropriate.
        //
        // NOW, doing this creates a different problem. Assuming a sufficiently asshole enough user we could indefinitely
        // extend the rounds by always submitting a guess within the extension window, so we need to cap this at a 
        // maximum number of extensions before we stop trying to solve the challenge above and just continue anyway.
        //
        // Clearly I am intending to one day add Wordle.Apps.Common support to this, and you know they'll be dicks about it.
        if (session.ActiveRoundEnd.Value.Subtract(TimeSpan.FromSeconds(options.RoundExtensionWindow))
            .IsOnOrBefore(_clock.UtcNow()))
        {
            long numExtensions = (int)
                Math.Floor(((session.ActiveRoundEnd - round.CreatedAt).Value.TotalSeconds - options.InitialRoundLength) /
                           options.RoundExtensionLength);

            if (numExtensions < options.MaximumRoundExtensions)
            {
                session.ActiveRoundEnd += TimeSpan.FromSeconds(options.RoundExtensionLength);
                await uow.Sessions.UpdateAsync(session);

                return true;
            }
        }

        return false;
    }
}