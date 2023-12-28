using MediatR;
using Queries;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;

namespace Wordle.CommandHandlers;

public class EndActiveRoundCommandHandler : IRequestHandler<EndActiveRoundCommand, Unit>
{
    private static readonly Random Random = new Random();
    private readonly IClock _clock;
    private readonly IGameUnitOfWorkFactory _uowFactory;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public EndActiveRoundCommandHandler(IClock clock, IGameUnitOfWorkFactory uowFactory, ILogger logger, IMediator mediator)
    {
        _clock = clock;
        _logger = logger;
        _uowFactory = uowFactory;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(EndActiveRoundCommand request, CancellationToken cancellationToken)
    {
        var res = await _mediator.Send(new GetSessionByIdQuery(request.SessionId));

        if (!res.HasValue)
        {
            throw new CommandException($"Cannot end active Round for Session {request.SessionId}, Session not found.");
        }

        var session = res.Value.Session;
        var options = res.Value.Options;
        var rounds = res.Value.Rounds;

        if (session.State != SessionState.ACTIVE)
        {
            throw new CommandException($"Cannot end active Round for Session {request.SessionId}, not ACTIVE.");
        }

        if (!session.ActiveRoundEnd.HasValue)
        {
            throw new CommandException(
                $"Cannot end active Round for Session {request.SessionId}, there is no round end time set.");
        }

        var round = rounds?.FirstOrDefault(x => x.Id == session.ActiveRoundId);
        if (round == null)
        {
            throw new CommandException(
                $"Cannot end active round for Session {request.SessionId}, the active Round {session.ActiveRoundId} not found.");
        }

        if (round.State != RoundState.ACTIVE)
        {
            throw new CommandException($"Cannot end active round with id {round.Id} as it is not ACTIVE.");
        }

        var guesses = await _mediator.Send(new GetGuessesForRoundQuery(round.Id), cancellationToken);
        
        var uow = _uowFactory.Create();
        
        if (!request.Force)
        {
            if (session.ActiveRoundEnd.Value.IsAfter(_clock.UtcNow()))
            {
                _logger.Log($"Attempting to end round {session.ActiveRoundId} but it is in the future.");
                throw new CommandException($"Cannot end active Round{session.ActiveRoundId} for Session {session.Id} as it is in the future.");
            }

            // firstly, check to see if we should actually end the round, because maybe there aren't enough guesses registered and
            // in that case we really should extend the round
            if (guesses.Count < options.MinimumAnswersRequired)
            {
                long numExtensions = (int)
                    Math.Floor(
                        ((_clock.UtcNow() - round.CreatedAt).TotalSeconds - options.InitialRoundLength) /
                        options.RoundExtensionLength);

                if (numExtensions < options.MaximumRoundExtensions)
                {
                    session.ActiveRoundEnd = session.ActiveRoundEnd.Value.Add(TimeSpan.FromSeconds(options.RoundExtensionLength));
                    await uow.Sessions.UpdateAsync(session);
                    await uow.SaveAsync();
                    
                    await _mediator.Publish(new RoundExtended(session.Id, round.Id, session.ActiveRoundEnd.Value), cancellationToken);
                    
                    _logger.Log($"Extending Round {round.Id} as the end criteria is unmet. Next check: {session.ActiveRoundEnd}");
                    return Unit.Value;
                }
            }
        }

        // it's possible to get here if there are no guesses in the round, and we've gone past the maximium number
        // of times the round can be extended. This really should be a termination scenario.
        if (guesses.Count == 0)
        {
            round.State = RoundState.TERMINATED;

            session.State = SessionState.TERMINATED;
            session.ActiveRoundId = null;
            session.ActiveRoundEnd = null;
            
            await uow.Rounds.UpdateAsync(round);
            await uow.Sessions.UpdateAsync(session);
            await uow.SaveAsync();

            await _mediator.Publish(new RoundTerminated(session.Id, round.Id), cancellationToken);
            await _mediator.Publish(new SessionTerminated(session.Id), cancellationToken);

            _logger.Log($"TERMINATING Round {round.Id} and Session {session.Id}");
            
            return Unit.Value;
        }

        var word = DetermineWordForRound(guesses, options);
        await UpdateRoundSelectedGuess(uow, round, word, session);

        await uow.SaveAsync();
        await _mediator.Publish(new RoundEnded(session.Id, round.Id), cancellationToken);
        
        _logger.Log($"Ending Round {round.Id}, selected word was {word}.");

        return Unit.Value;
    }


    private async Task UpdateRoundSelectedGuess(IGameUnitOfWork uow, Round round, KeyValuePair<string, List<Guess>> word, Session session)
    {
        round.Guess = word.Key;
        round.Result = new List<LetterState>();

        for (int i = 0; i < session.Word.Length; i++)
        {
            var correct = Char.ToLower(session.Word[i]);
            var guess = Char.ToLower(word.Key[i]);

            if (correct == guess)
            {
                round.Result.Add(LetterState.CORRECT_LETTER_CORRECT_POSITION);
            }
            else if (session.Word.Contains(guess))
            {
                round.Result.Add(LetterState.CORRECT_LETTER_INCORRECT_POSITION);
            }
            else
            {
                round.Result.Add(LetterState.INVALID);
            }
        }

        var usedLetters = new List<string>(session.UsedLetters);
        usedLetters.AddRange(word.Key.Select(x => x.ToString()).ToList());

        session.UsedLetters = usedLetters.Distinct().OrderBy(x => x).ToList(); 
        session.ActiveRoundId = null;
        session.ActiveRoundEnd = null;
        await uow.Sessions.UpdateAsync(session);
        
        round.State = RoundState.INACTIVE;
        await uow.Rounds.UpdateAsync(round);
    }

    public static KeyValuePair<string, List<Guess>> DetermineWordForRound(List<Guess> guesses, Options options)
    {
        // this is gonna get convoluted, but lets try and break this process down.
        // first of all, users have a maximum number of votes, so lets firstly trim them down to the maximum
        // by looking at the LAST entries they submitted, that means if they vote 10 times, and the limit per
        // round is two, we take the last 2 votes they submitted.
        var truncatedUserVotes = DecimateGuesses(guesses, options);

        // now we have a reduced subset of values, group the guesses by the word that was guessed
        // then pick the words that have the highest number of guesses, throwing everything else away
        // this should result in one entry that is a List of d
        var selectionPool = truncatedUserVotes
            // group by the actual word 
            .GroupBy(x => x.Word)
            .Select(x => new GuessContainer(x.Key, x.ToList()))
            // now sort the group by the number of guesses in it
            .GroupBy(x => x.Count)
            .OrderByDescending(x => x.Count())
            // extract the word/guess set that have the higest votes
            .Select(x => x.ToList())
            .First();

        if (selectionPool.Count() == 1)
        {
            var first = selectionPool.First();
            return new KeyValuePair<string, List<Guess>>(first.Word, first.Guesses);
        }

        // now we need to do some logic depending on how the game is configured in the case that we have multiple words with
        // the same number of votes from users
        switch (options.TiebreakerStrategy)
        {
            // in the case of a tie, select the word that was submitted the earliset in the game
            case TiebreakerStrategy.FIRST_IN:
            {
                return selectionPool
                    .OrderBy(x => x.EarliestVote.Timestamp)
                    .Select(x => new KeyValuePair<string, List<Guess>>(x.Word, x.Guesses))
                    .First();
            }

            // this is the opposite of the FIRST_IN case, this will retrieve the word that had the last vote added to it
            case TiebreakerStrategy.LAST_IN:
            {
                return selectionPool
                    .OrderByDescending(x => x.LatestVote.Timestamp)
                    .Select(x => new KeyValuePair<string, List<Guess>>(x.Word, x.Guesses))
                    .First();
            }

            default:
            {
                var list = selectionPool.ToList();
                var obj = list[Random.Next(0, list.Count())];
                return new KeyValuePair<string, List<Guess>>(obj.Word, obj.Guesses);
            }
        }
    }

    public static List<Guess> DecimateGuesses(List<Guess> guesses, Options options)
    {
        var truncatedUserVotes = guesses
            .GroupBy(x => x.User)
            .Select(x =>
                new KeyValuePair<string, List<Guess>>(x.Key, x
                    .OrderByDescending(y => y.Timestamp)
                    .DistinctBy(y => y.Word)
                    .Take(options.RoundVotesPerUser)
                    .ToList()
                ))
            // throw away the KVP now, we actually just want a raw list of Guesses now we know
            // we've filtered them to the user's most recent X.
            .SelectMany(x => x.Value)
            .ToList();
        return truncatedUserVotes;
    }

    private class GuessContainer
    {
        public string Word { get; set; }
        public List<Guess> Guesses { get; set; }

        public Guess EarliestVote => Guesses.OrderBy(x => x.Timestamp).First();
        public Guess LatestVote => Guesses.OrderBy(x => x.Timestamp).Last();

        public int Count => Guesses.Count();

        public GuessContainer(string word, List<Guess> guesses)
        {
            Word = word;
            Guesses = guesses;
        }
    }
}