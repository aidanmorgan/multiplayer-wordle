namespace Wordle.Model;

public enum SessionState
{
    INACTIVE = 0,
    ACTIVE = 1,
    SUCCESS = 2,
    FAIL = 4,
    TERMINATED = 8
}