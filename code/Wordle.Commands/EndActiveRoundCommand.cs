using MediatR;

namespace Wordle.Commands;

public class EndActiveRoundCommand : IRequest<Unit>
{
    public Guid SessionId { get; set; }
    
    public bool Force { get; set; }

    public EndActiveRoundCommand(Guid sessionId, bool force = false)
    {
        this.SessionId = sessionId;
        this.Force = force;
    }
}