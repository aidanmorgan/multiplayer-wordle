namespace Wordle.Model;

public enum TiebreakerStrategy
{
    // if there is a tie, just pick one of the options randomly
    RANDOM = 0,
    // if there is a tie, pick the one that was first entered
    FIRST_IN = 1,
    // if there is a tie, pick the one that was last entered
    LAST_IN = 2
}