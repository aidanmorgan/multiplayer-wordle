using System.Runtime.Serialization;
using Wordle.Model;

namespace Wordle.Events;

public class GuessAdded : BaseEvent
{
    public Guid GuessId { get; private set; }
    public Guid RoundId { get; private set; }
    public long RoundVersion { get; private set; }
    
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }

    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new VersionId<Session>(SessionId, SessionVersion);

    [IgnoreDataMember]
    public VersionId<Round> VersionedRound => new VersionId<Round>(RoundId, RoundVersion);

    public GuessAdded(string tenant, Guid guessId, Guid roundId, long roundVersion, Guid sessionId, long sessionVersion) : base(tenant)
    {
        GuessId = guessId;
        RoundId = roundId;
        RoundVersion = roundVersion;
        SessionId = sessionId;
        SessionVersion = sessionVersion;
    }
}