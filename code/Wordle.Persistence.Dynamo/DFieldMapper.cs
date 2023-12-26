using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using MediatR;

namespace Wordle.Persistence.Dynamo;

public class DFieldMapper<T>
{
    public Func<T, Task<DynamoDBEntry>> ConvertToDynamo { get; private set; }
    public Func<DynamoDBEntry, T, Task> ConvertFromDynamo { get; private set; }
    
    public string Name { get; private set; }

    public DFieldMapper(string name, Func<T, Task<DynamoDBEntry>> o, Func<DynamoDBEntry, T, Task> i)
    {
        Name = name;
        ConvertToDynamo = o;
        ConvertFromDynamo = i;
    }
}