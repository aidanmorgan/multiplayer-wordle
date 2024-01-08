using MediatR;

namespace Wordle.Commands;

public class AddGuessToRoundCommand : IRequest<Unit>
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }
    public String User { get; private set; }
    public String Word { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    public AddGuessToRoundCommand(Guid sessionId, long sessionVersion, string user, string word, DateTimeOffset timestamp)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        User = user;
        Word = word;
        Timestamp = timestamp;
    }
}