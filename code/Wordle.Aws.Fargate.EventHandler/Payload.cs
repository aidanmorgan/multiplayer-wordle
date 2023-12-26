using Wordle.Events;

namespace Aws.Fargate.EventHandler;

public class Payload
{
    public Guid SessionId { get; set; }
    
    public Guid RoundId { get; set; }


    public static Payload Create(NewRoundStarted ev)
    {
        return  new Payload()
        {
            RoundId = ev.RoundId,
            SessionId = ev.SessionId
        };
    }
    
    public static Payload Create(RoundExtended ev)
    {
        return  new Payload()
        {
            RoundId = ev.RoundId,
            SessionId = ev.SessionId
        };
    }    
}