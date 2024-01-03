namespace Wordle.Persistence.DynamoDb;

public interface IDynamoRepository<T> : IRepository<T> where T : class, new()
{
    Task SaveAsync(DynamoGameConfiguration configuration);
}