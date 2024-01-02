using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Wordle.Model;

namespace Wordle.Persistence.Dynamo;

public class DynamoRepositoryImpl<T> : IDynamoRepository<T> where T : class, IAggregate, new()
{
    private readonly IList<Func<Table, Task>> _tasks = new List<Func<Table, Task>>();

    private readonly TableMapper<T> _tableMapper;

    public DynamoRepositoryImpl(TableMapper<T> tableMapper)
    {
        this._tableMapper = tableMapper;
    }

    public Task AddAsync(T val)
    {
        _tasks.Add( async (table) =>
        {
            var entry = await _tableMapper.ToDynamoAsync(val);
            await table.PutItemAsync(entry);
        });
        
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T val)
    {
        _tasks.Add( async (table) =>
        {
            var entry = await _tableMapper.ToDynamoAsync(val);
            await table.UpdateItemAsync(entry);
        });
        
        return Task.CompletedTask;    
    }
    
    public async Task SaveAsync(DynamoGameConfiguration config)
    {
        Table table = config.GetTable();
        
        foreach (var func in _tasks)
        {
            await func.Invoke(table);
        }
    }
}