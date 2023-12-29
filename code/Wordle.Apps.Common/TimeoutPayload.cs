using Wordle.Events;

namespace Wordle.Apps.Common;

public class TimeoutPayload
{
    public Guid SessionId { get; set; }
    
    public Guid RoundId { get; set; }

    public TimeoutPayload(Guid sessionId, Guid roundId)
    {
        SessionId = sessionId;
        RoundId = roundId;
    }


    public static TimeoutPayload Create(NewRoundStarted ev)
    {
        return new TimeoutPayload(ev.SessionId, ev.RoundId);
    }
    
    public static TimeoutPayload Create(RoundExtended ev)
    {
        return new TimeoutPayload(ev.SessionId, ev.RoundId);
    }    
}