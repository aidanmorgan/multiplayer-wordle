using Dapper;
using MediatR;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetGuessesForRoundQueryHandler : IRequestHandler<GetGuessesForRoundQuery, List<Guess>>
{
    private readonly WordleEfCoreSettings _context;

    public GetGuessesForRoundQueryHandler(WordleEfCoreSettings context)
    {
        _context = context;
    }

    public async Task<List<Guess>> Handle(GetGuessesForRoundQuery request, CancellationToken cancellationToken)
    {
        if (request.IgnoreAfter.HasValue)
        {
            var guesses = await _context.Connection.QueryAsync<Guess>(
                "SELECT * FROM guesses WHERE roundid = @id AND timestamp <= @time",
                new
                {
                    Id = request.RoundId,
                    Time = request.IgnoreAfter.Value
                }
            );

            return guesses?.ToList() ?? [];
        }
        else
        {
            var guesses = await _context.Connection.QueryAsync<Guess>(
                "SELECT * FROM guesses WHERE roundid = @id",
                new
                {
                    Id = request.RoundId
                });

            return guesses.ToList() ?? [];
        }
    }
}