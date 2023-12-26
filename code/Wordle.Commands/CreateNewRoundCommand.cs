using MediatR;

namespace Wordle.Commands;

public class CreateNewRoundCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }

    public CreateNewRoundCommand(Guid sessionId)
    {
        this.SessionId = sessionId;
    }
}