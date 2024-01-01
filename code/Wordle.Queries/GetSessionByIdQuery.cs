using MediatR;
using Wordle.Model;

namespace Queries;

public class SessionQueryResult
{
    public Session Session { get; set; }
    public List<Round> Rounds { get; set; }
    public Options Options { get; set; }
}

public class GetSessionByIdQuery : IRequest<SessionQueryResult?>
{
    public Guid Id { get; set; }
    public bool IncludeOptions { get; set; } = true;
    public bool IncludeRounds { get; set; } = true;

    public bool IncludeWord { get; set; } = true;
    
    public GetSessionByIdQuery()
    {
        
    }

    public GetSessionByIdQuery(Guid id)
    {
        this.Id = id;
    }
}