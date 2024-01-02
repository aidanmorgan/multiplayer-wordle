using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EntityFramework;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EntityFramework;

public class GetGuessesForRoundQueryHandler : IRequestHandler<GetGuessesForRoundQuery, List<Guess>>
{
    private readonly WordleContext _context;

    public GetGuessesForRoundQueryHandler(WordleContext context)
    {
        _context = context;
    }

    public Task<List<Guess>> Handle(GetGuessesForRoundQuery request, CancellationToken cancellationToken)
    {
        var guesses = _context
            .Guesses
            .Where(x => x.RoundId == request.RoundId)
            .ToList();

        return Task.FromResult(guesses);
    }
}