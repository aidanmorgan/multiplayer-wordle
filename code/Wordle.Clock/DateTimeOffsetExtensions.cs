namespace Wordle.Clock;

public static class DateTimeOffsetExtensions
{
    public static bool IsAfter(this DateTimeOffset a, DateTimeOffset b)
    {
        return a.Subtract(b).TotalMilliseconds >= 0;
    }

    public static bool IsOnOrBefore(this DateTimeOffset a, DateTimeOffset b)
    {
        return a.Subtract(b).TotalMilliseconds <= 0;
    }
}