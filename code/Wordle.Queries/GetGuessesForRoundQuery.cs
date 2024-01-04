using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public class GetGuessesForRoundQuery : IRequest<List<Guess>> 
{
    public Guid RoundId { get; init; }
    public DateTimeOffset? IgnoreAfter { get; init; }


    public GetGuessesForRoundQuery()
    {
        
    }

    public GetGuessesForRoundQuery(Guid roundId, DateTimeOffset? ignoreAfter = null)
    {
        this.RoundId = roundId;
        this.IgnoreAfter = ignoreAfter;
    }

}