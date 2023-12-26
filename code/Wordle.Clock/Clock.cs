namespace Wordle.Clock;

public class Clock : IClock
{
    public DateTimeOffset UtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}