using MediatR;
using Wordle.Model;

namespace Queries;

public struct SessionQueryResult
{
    public Session Session;
    public List<Round> Rounds;
    public Options Options;
}

public class GetSessionByIdQuery : IRequest<SessionQueryResult?>
{
    public Guid Id { get; set; }
    public bool IncludeOptions { get; set; } = true;
    public bool IncludeRounds { get; set; } = true;
    
    public GetSessionByIdQuery()
    {
        
    }

    public GetSessionByIdQuery(Guid id)
    {
        this.Id = id;
    }
}