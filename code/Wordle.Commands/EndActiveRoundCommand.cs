using MediatR;

namespace Wordle.Commands;

public class EndActiveRoundCommand : IRequest<Unit>
{
    public Guid SessionId { get; set; }
    
    public Guid? RoundId { get; set; }
    
    public bool Force { get; set; }

    public EndActiveRoundCommand(Guid sessionId, Guid? roundId = null, bool force = false)
    {
        this.SessionId = sessionId;
        this.RoundId = roundId;
        this.Force = force;
    }
}