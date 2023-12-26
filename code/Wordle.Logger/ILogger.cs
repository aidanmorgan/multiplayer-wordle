namespace Wordle.Logger;

public interface ILogger
{
    void Log(string message);
    void Log(string level, string message);
}