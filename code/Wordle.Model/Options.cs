namespace Wordle.Model;

public class Options : IAggregate
{
    // values below are based on either what wordle does by default, or are what "makes a sensible default" for
    // playing multiplayer wordle.
    
    public DateTimeOffset CreatedAt { get; set; }
    public int InitialRoundLength { get; set; } = (1 * 60); // 1 minute rounds
    public int RoundExtensionWindow { get; set; } = 15;  // only extend the round if a guess comes in within this many seconds of the end of the round
    public int RoundExtensionLength { get; set; } = 30; // when a guess is added, how much to extend the round by
    public int MaximumRoundExtensions { get; set; } = 50; // the maximum number of times the round can be extended
    
    public int MinimumAnswersRequired { get; set; } = 1; // the minimum number of answers required to end a round
    public string DictionaryName { get; set; } = "wordle"; // the name of the dictionary to use
    
    // how many guesses are allowed per-user per-round
    public int RoundVotesPerUser { get; set; } = 2;  // the number of votes a user can make per round
    
    // setting this to FIRST_IN as there should be a "reward" for entering a word first
    public TiebreakerStrategy TiebreakerStrategy { get; set; } = TiebreakerStrategy.FIRST_IN;

    public int MaximumSessionDuration => (InitialRoundLength + (MaximumRoundExtensions * RoundExtensionLength) * NumberOfRounds);
    
    public int NumberOfRounds { get; set; } = 6; // how many rounds can be played
    public int WordLength { get; set; } = 5; // what is the length of the words that we are playing
    
    // TODO : work out if this is really required.
    public bool AllowGuessesAfterRoundEnd { get; set; } = true;
    public Guid? SessionId { get; set; }
    
    public string? TenantId { get; set; }
    public Guid Id { get; set; }
    
    // How many seconds before/after the end of the round should we tolerate as being part of the round
    public int RoundEndToleranceSeconds { get; set; } = 1;

    public bool IsTenant()
    {
        return TenantId != null && SessionId == null;
    }

    public bool IsSession()
    {
        return TenantId == null && SessionId != null;
    }

    public Options Clone()
    {
        // DO NOT copy Id values here, otherwise logic will break.
        // DO put all other values tho
        return new Options()
        {
            DictionaryName = this.DictionaryName,
            TiebreakerStrategy = this.TiebreakerStrategy,
            WordLength = this.WordLength,
            InitialRoundLength = this.InitialRoundLength,
            MaximumRoundExtensions = this.MaximumRoundExtensions,
            MinimumAnswersRequired = this.MinimumAnswersRequired,
            NumberOfRounds = this.NumberOfRounds,
            RoundExtensionLength = this.RoundExtensionLength,
            RoundExtensionWindow = this.RoundExtensionWindow,
            RoundVotesPerUser = this.RoundVotesPerUser,
            AllowGuessesAfterRoundEnd = this.AllowGuessesAfterRoundEnd,
            RoundEndToleranceSeconds = this.RoundEndToleranceSeconds
        };
    }
}