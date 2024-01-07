using MediatR;
using Microsoft.Extensions.Logging;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Queries;

namespace Wordle.CommandHandlers;

public class EndActiveRoundCommandHandler : IRequestHandler<EndActiveRoundCommand, Unit>
{
    private static readonly Random Random = new Random();
    private readonly IClock _clock;
    private readonly IGameUnitOfWorkFactory _uowFactory;
    private readonly IMediator _mediator;
    private readonly IGuessDecimator _guessDecimator;
    private readonly ILogger<EndActiveRoundCommandHandler> _logger;

    public EndActiveRoundCommandHandler(IClock clock, IGameUnitOfWorkFactory uowFactory, IGuessDecimator decimator, ILogger<EndActiveRoundCommandHandler> logger, IMediator mediator)
    {
        _clock = clock;
        _logger = logger;
        _uowFactory = uowFactory;
        _guessDecimator = decimator;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(EndActiveRoundCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow();
        
        var res = await _mediator.Send(new GetSessionByIdQuery(request.SessionId));
        if (res == null)
        {
            throw new EndActiveRoundCommandException($"Cannot end active Round for Session {request.SessionId}, Session not found.");
        }

        var session = res.Session;
        var options = res.Options;
        var rounds = res.Rounds;

        if (session.State != SessionState.ACTIVE)
        {
            throw new EndActiveRoundCommandException($"Cannot end active Round for Session {request.SessionId}, not ACTIVE.");
        }

        if (!session.ActiveRoundEnd.HasValue)
        {
            throw new EndActiveRoundCommandException(
                $"Cannot end active Round for Session {request.SessionId}, there is no round end time set.");
        }

        if (request.RoundId != null && session.ActiveRoundId != request.RoundId)
        {
        }

        Round? round = null;

        if (request.RoundId != null)
        {
            if (session.ActiveRoundId != request.RoundId)
            {
                throw new EndActiveRoundCommandException(
                    $"Cannot end active Round for Session {request.SessionId}, the expected Round {request.RoundId} is not the active round.");
            }

            round = res.Rounds.FirstOrDefault(x => x.Id == request.RoundId);
        }
        else
        {
            round = rounds?.FirstOrDefault(x => x.Id == session.ActiveRoundId);
        } 
        
        if (round == null)
        {
            throw new EndActiveRoundCommandException(
                $"Cannot end active round for Session {request.SessionId}, the active Round {session.ActiveRoundId} not found.");
        }

        if (round.State != RoundState.ACTIVE)
        {
            throw new EndActiveRoundCommandException($"Cannot end active round with id {round.Id} as it is not ACTIVE.");
        }

        var roundEndWithTolerance = session.ActiveRoundEnd.Value.Add(TimeSpan.FromSeconds(options.RoundEndToleranceSeconds));
        var guesses = await _mediator.Send(new GetGuessesForRoundQuery(round.Id, roundEndWithTolerance), cancellationToken);
        var uow = _uowFactory.Create();
        
        if (!request.Force)
        {
            // if we are attempting to end the round but the check comes in "slightly" early, then we should just
            // allow it to happen anyway.
            var secondsInFuture = session.ActiveRoundEnd.Value.Subtract(now).TotalSeconds;
            if ( secondsInFuture> options.RoundEndToleranceSeconds)
            {
                throw new EndActiveRoundCommandException($"Cannot end active Round{session.ActiveRoundId} for Session {session.Id} as it is {secondsInFuture} seconds in the future.");
            }

            // BUG:
            // A similar form of this logic used to be in the add guess command, but it creates an interesting race condition with this
            // attempt to cancel if the processing time of this task takes longer than it takes to add a guess, in the other
            // way of doing it the round would be extended by the guess being added, but then the round is attempted to be
            // updated again here to a different value. Performing this check here also makes a bit more sense as we now extend
            // the round for the extension period from now, rather than from when a guess was added which isn't necessarily
            // a smart choice.
            //
            // I need to play the game a bunch more times to try and determine which version of the logic "feels" better,
            // but from a purely logical (and also trying to address a defect) scenario this does seem to make the most 
            // sense - mainly because now the round end time is determined in only one place. SRP baby.
            DateTimeOffset? lastGuess = guesses.Count > 0
                ? guesses
                    .Where(x => x.Timestamp < session.ActiveRoundEnd.Value.Add(TimeSpan.FromSeconds(options.RoundEndToleranceSeconds)))
                    .Select(x => x.Timestamp)
                    .OrderDescending()
                    .First()
                : null;
            
            double differenceToRoundEnd = double.MaxValue;
            if (lastGuess.HasValue && session.ActiveRoundEnd.Value > lastGuess)
            {
                differenceToRoundEnd = session.ActiveRoundEnd.Value.Subtract(lastGuess.Value).TotalSeconds;
            }
            
            // firstly, check to see if we should actually end the round, because maybe there aren't enough guesses registered and
            // in that case we really should extend the round.
            //
            // We also add an additional check here that tries to see if the last guess in the game came in within the extension
            // window of the round, because if it did then we need to extend the round anyway as people will need more time to
            // decide if they want to vote, or if they want to add a different word.
            if (guesses.Count < options.MinimumAnswersRequired || differenceToRoundEnd <= options.RoundExtensionWindow)
            {
                long numExtensions = (int)
                    Math.Floor(
                        ((now - round.CreatedAt).TotalSeconds - options.InitialRoundLength) /
                        options.RoundExtensionLength);
                
                var maximumRoundDuration = options.InitialRoundLength + (options.RoundExtensionLength * options.MaximumRoundExtensions);

                // if there are no guesses make the initial round the same legnth again, otherwise use the extension window only
                var extension = guesses.Count == 0 ? options.InitialRoundLength : options.RoundExtensionLength;
                session.ActiveRoundEnd = now.Add(TimeSpan.FromSeconds(extension));
                
                // if we are allowed to add another extension AND adding the extension doesn't make the round go longer than it is allowed to
                // then add the extension to the round
                if (numExtensions < options.MaximumRoundExtensions && session.ActiveRoundEnd.Value < (round.CreatedAt.AddSeconds(maximumRoundDuration)))
                {
                    // we have a valid reason to extend the round, so lets make it move into the future.
                    await uow.Sessions.UpdateAsync(session);
                    await uow.SaveAsync();
                    
                    if (guesses.Count < options.MinimumAnswersRequired)
                    {
                        await _mediator.Publish(new RoundExtended(session.Tenant, session.Id, round.Id, session.ActiveRoundEnd.Value, RoundExtensionReason.NOT_ENOUGH_GUESSES), cancellationToken);

                        _logger.LogInformation(
                            "Extending Round {RoundId} as the end criteria is unmet. Next check: {SessionActiveRoundEnd}",
                            round.Id, session.ActiveRoundEnd);
                    }
                    else if (differenceToRoundEnd <= options.RoundExtensionWindow)
                    {
                        await _mediator.Publish(new RoundExtended(session.Tenant, session.Id, round.Id, session.ActiveRoundEnd.Value, RoundExtensionReason.LATE_ARRIVING_GUESS), cancellationToken);

                        _logger.LogInformation(
                            "Extending Round {RoundId} as the last guess was submitted at {GuessTime} which was within the {Window} limit. Next check: {SessionActiveRoundEnd}",
                            round.Id, lastGuess, options.RoundEndToleranceSeconds, session.ActiveRoundEnd);

                    }

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

            await _mediator.Publish(new RoundTerminated(session.Tenant, session.Id, round.Id), cancellationToken);
            await _mediator.Publish(new SessionTerminated(session.Tenant, session.Id), cancellationToken);

            _logger.LogInformation("TERMINATING Round {RoundId} and Session {SessionId}", round.Id, session.Id);
            
            return Unit.Value;
        }

        var word = DetermineWordForRound(_guessDecimator, guesses, options);
        await UpdateRoundSelectedGuess(uow, round, word, session);

        await uow.SaveAsync();
        await _mediator.Publish(new RoundEnded(session.Tenant, session.Id, round.Id), cancellationToken);
        
        _logger.LogInformation("Ending Round {RoundId}, selected word was {Word}.", round.Id, word.Key);

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

    public static KeyValuePair<string, List<Guess>> DetermineWordForRound(IGuessDecimator decimator, List<Guess> guesses, Options options)
    {
        // this is gonna get convoluted, but lets try and break this process down.
        // first of all, users have a maximum number of votes, so lets firstly trim them down to the maximum
        // by looking at the LAST entries they submitted, that means if they vote 10 times, and the limit per
        // round is two, we take the last 2 votes they submitted.
        var truncatedUserVotes =  decimator.DecimateGuesses(guesses, options);

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