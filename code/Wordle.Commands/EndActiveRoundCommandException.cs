namespace Wordle.Commands;

public class EndActiveRoundCommandException : CommandException
{
    public TimeSpan? RetryAfter { get; set; } = null;
    
    public EndActiveRoundCommandException(string? message, TimeSpan? retryAfter = null) : base(message)
    {
        RetryAfter = retryAfter;
    }
}