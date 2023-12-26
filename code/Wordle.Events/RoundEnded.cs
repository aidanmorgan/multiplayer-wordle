using MediatR;

namespace Wordle.Events;

public class RoundEnded : IEvent
{
    public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();

    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }

    public Guid SessionId { get; set; }
    public Guid RoundId { get; set; }

    public RoundEnded(Guid sessionId, Guid roundId)
    {
        SessionId = sessionId;
        RoundId = roundId;
    }
}