using Wordle.EfCore;

namespace Wordle.Persistence.EfCore;

public class EfUnitOfWorkFactory : IGameUnitOfWorkFactory
{
    private readonly WordleEfCoreSettings _settings;

    public EfUnitOfWorkFactory(WordleEfCoreSettings settings)
    {
        _settings = settings;
    }

    public IGameUnitOfWork Create()
    {
        return new EfGameUnitOfWork(_settings.DbContext);
    }
}