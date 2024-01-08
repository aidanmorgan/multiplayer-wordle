using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public class SessionQueryResult
{
    public Session Session { get; set; }
    public List<Round> Rounds { get; set; }
    public Options Options { get; set; }
}

public class GetSessionByIdQuery : IRequest<SessionQueryResult?>
{
    public Guid Id { get; set; }
    public long Version { get; set; }

    public bool VersionAware { get; set; } = true;
    
    public bool IncludeOptions { get; set; } = true;
    public bool IncludeRounds { get; set; } = true;

    public bool IncludeWord { get; set; } = true;
    
    public GetSessionByIdQuery()
    {
        
    }

    public GetSessionByIdQuery(Guid id, long? version)
    {
        this.Id = id;

        if (version.HasValue)
        {
            VersionAware = true;
            Version = version.Value;
        }
        else
        {
            VersionAware = false;
        }
    }
}