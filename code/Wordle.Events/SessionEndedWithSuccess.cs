using System.Runtime.Serialization;
using Wordle.Model;

namespace Wordle.Events;

public class SessionEndedWithSuccess : BaseEvent
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }
    
    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new(SessionId, SessionVersion);

    
    public SessionEndedWithSuccess(string tenant, Guid id, long sessionVersion) : base(tenant)
    {
        SessionId = id;
        SessionVersion = sessionVersion;
    }
}