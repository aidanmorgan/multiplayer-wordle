using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Wordle.Model;

namespace Wordle.Events;

public class NewSessionStarted : BaseEvent
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }

    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new VersionId<Session>(SessionId, SessionVersion);

    public NewSessionStarted(string tenant, Guid id, long version) : base(tenant)
    {
        SessionId = id;
        SessionVersion = version;
    }
}