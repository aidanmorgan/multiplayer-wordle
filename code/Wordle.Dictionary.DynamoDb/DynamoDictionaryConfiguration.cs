using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Wordle.Dictionary.DynamoDb;

public class DynamoDictionaryConfiguration
{
    private IAmazonDynamoDB DynamoContext { get; set; }
    private string TableName { get; set; }

    private Table? _table;
    
    public DynamoDictionaryConfiguration(IAmazonDynamoDB dynamoContext, string tableName)
    {
        DynamoContext = dynamoContext;
        TableName = tableName;
    }

    public Table GetTable()
    {
        return _table ??= Table.LoadTable(DynamoContext, TableName);
    }
}