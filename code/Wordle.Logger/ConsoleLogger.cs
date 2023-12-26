namespace Wordle.Logger;
 
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }

    public void Log(string level, string message)
    {
        Console.WriteLine($"[{level}]: {message}");
    }
}