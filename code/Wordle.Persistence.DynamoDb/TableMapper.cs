using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace Wordle.Persistence.Dynamo;

public class TableMapper<T> where T : new()
{
    private readonly IList<DFieldMapper<T>> _fieldMapping;

    public List<string> ColumnNames => _fieldMapping.Select(x => x.Name).ToList();

    public TableMapper(IList<DFieldMapper<T>> fieldMapping)
    {
        _fieldMapping = fieldMapping;
    }
    
    public  async Task<T> FromDynamoAsync(Document dictionary)
    {
        T result = new T();

        foreach (var field in _fieldMapping)
        {
            if (dictionary.ContainsKey(field.Name))
            {
                await field.ConvertFromDynamo(dictionary[field.Name], result);
            }
        }

        return result;
    }

    public async Task<Document> ToDynamoAsync(T val)
    {
        var res = new Document();
        
        foreach (var field in _fieldMapping)
        {
            res[field.Name] = await field.ConvertToDynamo(val);
        }

        return res;
    }
}