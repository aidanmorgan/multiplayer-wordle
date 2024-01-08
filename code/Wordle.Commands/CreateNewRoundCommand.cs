using MediatR;

namespace Wordle.Commands;

public class CreateNewRoundCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    
    public long SessionVersion { get; set; }

    public CreateNewRoundCommand(Guid sessionId, long sessionVersion)
    {
        this.SessionId = sessionId;
        SessionVersion = sessionVersion;
    }
}