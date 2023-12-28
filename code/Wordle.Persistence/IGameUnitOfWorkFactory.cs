namespace Wordle.Persistence;

public interface IGameUnitOfWorkFactory
{
    IGameUnitOfWork Create();
}