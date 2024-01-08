using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public struct SessionAndRound
{
    public VersionId Session { get; set; }
    public VersionId Round { get; set; }
    
    public DateTimeOffset? RoundExpiry { get; set; }
}
public class GetAllActiveSessionsQuery : IRequest<List<SessionAndRound>> 
{
    
}