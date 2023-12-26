namespace Wordle.Clock;

public interface IClock
{
    DateTimeOffset UtcNow();
}