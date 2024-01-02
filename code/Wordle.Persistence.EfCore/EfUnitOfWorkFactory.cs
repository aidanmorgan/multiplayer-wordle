using Wordle.EfCore;

namespace Wordle.Persistence.EfCore;

public class EfUnitOfWorkFactory : IGameUnitOfWorkFactory
{
    private readonly string _connectionString;

    public EfUnitOfWorkFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IGameUnitOfWork Create()
    {
        return new EfGameUnitOfWork(new WordleContext(_connectionString));
    }
}