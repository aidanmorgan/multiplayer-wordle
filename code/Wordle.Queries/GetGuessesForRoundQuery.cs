using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public class GetGuessesForRoundQuery : IRequest<List<Guess>> 
{
    public Guid RoundId { get; set; }

    public GetGuessesForRoundQuery()
    {
        
    }

    public GetGuessesForRoundQuery(Guid roundId)
    {
        this.RoundId = roundId;
    }
}