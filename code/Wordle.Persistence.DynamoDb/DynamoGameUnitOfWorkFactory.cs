namespace Wordle.Persistence.DynamoDb;

public class DynamoGameUnitOfWorkFactory : IGameUnitOfWorkFactory
{
    private readonly DynamoGameConfiguration _dynamo;
    private readonly IDynamoMappers _mappers;

    public DynamoGameUnitOfWorkFactory(DynamoGameConfiguration config, IDynamoMappers mappers)
    {
        this._dynamo = config;
        this._mappers = mappers;
    }

    public IGameUnitOfWork Create()
    {
        return new DynamoGameUnitOfWork(_dynamo, _mappers);
    }
}