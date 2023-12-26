namespace Wordle.Commands;

public class CommandException : Exception
{
    public CommandException(string? message)
        : base(message)
    {
    }
}