using MediatR;

namespace Wordle.Commands;

public class EndActiveRoundCommand : IRequest<Unit>
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
    
    public Guid? RoundId { get; set; }
    public long? RoundVersion { get; set; }
    
    public bool Force { get; set; }

    public EndActiveRoundCommand(Guid sessionId, long sessionVersion, Guid? roundId = null, long? roundVersion = null, bool force = false)
    {
        this.SessionId = sessionId;
        this.SessionVersion = sessionVersion;
        this.RoundId = roundId;
        this.RoundVersion = roundVersion;
        this.Force = force;
    }
}