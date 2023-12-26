using Amazon.DynamoDBv2;

namespace Wordle.Persistence.Dynamo;

public interface IDynamoRepository<T> : IRepository<T> where T : class, new()
{
    Task SaveAsync(DynamoGameConfiguration configuration);
}