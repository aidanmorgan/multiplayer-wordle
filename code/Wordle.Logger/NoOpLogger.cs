namespace Wordle.Logger;

public class NoOpLogger : ILogger
{
    public void Log(string message)
    {
        
    }

    public void Log(string level, string message)
    {
        
    }
}