namespace Wordle.Model;

public class Options : IAggregate
{
    public DateTimeOffset CreatedAt { get; set; }
    public int InitialRoundLength { get; set; } = (1 * 60); // 1 minute rounds
    public int RoundExtensionWindow { get; set; } = 10;  // only extend the round if a guess comes in within this many seconds of the end of the round
    public int RoundExtensionLength { get; set; } = 30; // when a guess is added, how much to extend the round by
    public int MaximumRoundExtensions { get; set; } = 50; // the maximum number of times the round can be extended
    
    public int MinimumAnswersRequired { get; set; } = 1; // the minimum number of answers required to end a round
    public string DictionaryName { get; set; } = "wordle"; // the name of the dictionary to use
    public int RoundVotesPerUser { get; set; } = 2;  // the number of votes a user can make per round
    public TiebreakerStrategy TiebreakerStrategy { get; set; } = TiebreakerStrategy.LAST_IN;

    public int MaximumSessionDuration => (InitialRoundLength + (MaximumRoundExtensions * RoundExtensionLength)) * NumberOfRounds;

    
    public int NumberOfRounds { get; set; } = 6; // how many rounds can be played
    public int WordLength { get; set; } = 5;
    
    
    public bool AllowGuessesAfterRoundEnd { get; set; } = true;
    public Guid? SessionId { get; set; }
    
    public string? TenantId { get; set; }
    public Guid Id { get; set; }
    public int RoundEndToleranceSeconds { get; set; } = 2;

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
            AllowGuessesAfterRoundEnd = this.AllowGuessesAfterRoundEnd
        };
    }
}