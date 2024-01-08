using System.Runtime.Serialization;
using Wordle.Model;

namespace Wordle.Events;

public class SessionTerminated : BaseEvent
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }
    
    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new(SessionId, SessionVersion);


    public SessionTerminated(string tenant, Guid sessionId, long sessionVersion) : base(tenant)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
    }
}