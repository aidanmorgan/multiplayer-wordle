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
    
    // this is a workaround to allow the same query to be used for querying when we want to add a guess to a session
    // adding guesses doesn't change any of the version numbers, they are effectively immutable
    public bool SpecificVersionRequired { get; set; } = true;
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
            SpecificVersionRequired = true;
            Version = version.Value;
        }
        else
        {
            SpecificVersionRequired = false;
        }
    }
}