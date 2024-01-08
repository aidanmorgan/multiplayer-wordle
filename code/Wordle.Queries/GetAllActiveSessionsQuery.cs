using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public class ActiveSessionResult
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
    
    public Guid RoundId { get; set; }
    public long RoundVersion { get; set; }
    
    public DateTimeOffset RoundExpiry { get; set; }

    public VersionId<Session> VersionedSession => new VersionId<Session>()
    {
        Id = SessionId,
        Version = SessionVersion
    };

    public VersionId<Round> VersionedRound => new VersionId<Round>()
    {
        Id = RoundId,
        Version = RoundVersion
    };
}

public class GetAllActiveSessionsQuery : IRequest<List<ActiveSessionResult>> 
{
    
}