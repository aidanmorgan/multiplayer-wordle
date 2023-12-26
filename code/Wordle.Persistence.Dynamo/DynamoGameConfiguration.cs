using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Wordle.Persistence.Dynamo;

public class DynamoGameConfiguration
{
    public string TableName { get; set; }
    public IAmazonDynamoDB Context { get; set; }

    public DynamoGameConfiguration(IAmazonDynamoDB context, string tableName)
    {
        TableName = tableName;
        Context = context;
    }

    private Table _table;
    public Table GetTable()
    {
        return _table ??= Table.LoadTable(Context, TableName);
    }
}