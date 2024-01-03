using Amazon.DynamoDBv2.DocumentModel;

namespace Wordle.Persistence.DynamoDb;

public static class QueryFilterExtensions
{
    public static QueryFilter ChainCondition(this QueryFilter filter, string arg, QueryOperator op, params DynamoDBEntry[] values)
    {
        filter.AddCondition(arg, op, values);
        return filter;
    }
}