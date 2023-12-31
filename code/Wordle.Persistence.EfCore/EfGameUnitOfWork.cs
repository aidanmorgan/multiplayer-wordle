using Wordle.EfCore;
using Wordle.Model;

namespace Wordle.Persistence.EfCore;

public class EfGameUnitOfWork : IGameUnitOfWork
{
    private readonly WordleContext _context;

    public EfGameUnitOfWork(WordleContext wordleContext)
    {
        _context = wordleContext;
    }

    private ISessionRepository? _sessionRepository;
    public ISessionRepository Sessions
    {
        get
        {
            _sessionRepository ??= new EfSessionRepository(_context.Sessions);
            return _sessionRepository;
        }
    }

    private EfRepository<Guess>? _guessRepository;
    public IRepository<Guess> Guesses
    {
        get
        {
            _guessRepository ??= new EfRepository<Guess>(_context.Guesses);
            return _guessRepository;
        }
    }

    private EfRepository<Round>? _roundRepository;

    public IRepository<Round> Rounds
    {
        get
        {
            _roundRepository ??= new EfRepository<Round>(_context.Rounds);
            return _roundRepository;
        }
    }

    private EfOptionsRepository? _optionsRepository;

    public IOptionsRepository Options
    {
        get
        {
            _optionsRepository ??= new EfOptionsRepository(_context.Options);
            return _optionsRepository;
        }
    }


    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}