namespace Wordle.Model;

public interface IGuessDecimator
{
    public List<Guess> DecimateGuesses(IList<Guess> guesses, Options options);
}

public class GuessDecimator : IGuessDecimator
{
    public List<Guess> DecimateGuesses(IList<Guess> guesses, Options options)
    {
        var truncatedUserVotes = guesses
            .AsParallel()
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
}