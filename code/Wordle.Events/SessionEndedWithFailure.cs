using System.Runtime.Serialization;
using Wordle.Model;

namespace Wordle.Events;

public class SessionEndedWithFailure : BaseEvent
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
 
    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new(SessionId, SessionVersion);


    public SessionEndedWithFailure(string tenant, Guid id, long sessionVersion) : base(tenant)
    {
        SessionId = id;
        SessionVersion = sessionVersion;
    }
}