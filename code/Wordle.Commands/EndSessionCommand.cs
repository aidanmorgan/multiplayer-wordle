using MediatR;

namespace Wordle.Commands;

public class EndSessionCommand : IRequest<Unit>
{
    public Guid SessionId { get; private set; }
    public string Word { get; private set; }
    public bool Success { get; private set; } = false;
    public bool Fail { get; private set; } = false;

    public EndSessionCommand(Guid sessionId, string word, bool success, bool fail)
    {
        SessionId = sessionId;
        Word = word;
        Success = success;
        Fail = fail;
    }
}