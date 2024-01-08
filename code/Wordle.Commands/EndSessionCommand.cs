using MediatR;

namespace Wordle.Commands;

public class EndSessionCommand : IRequest<Unit>
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }
    public string Word { get; private set; }
    public bool Success { get; private set; } = false;
    public bool Fail { get; private set; } = false;

    public EndSessionCommand(Guid sessionId, long sessionVersion, string word, bool success, bool fail)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        Word = word;
        Success = success;
        Fail = fail;
    }
}