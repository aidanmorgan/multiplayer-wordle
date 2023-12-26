using MediatR;
using Wordle.Model;

namespace Wordle.Persistence;

public interface IGameUnitOfWork
{
    ISessionRepository Sessions { get; }
    
    IRepository<Guess> Guesses { get; }
    
    IRepository<Round> Rounds { get; }
    
    IOptionsRepository Options { get; }

    Task SaveAsync();
}