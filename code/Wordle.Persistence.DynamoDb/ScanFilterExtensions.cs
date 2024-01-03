using Amazon.DynamoDBv2.DocumentModel;

namespace Wordle.Persistence.DynamoDb;

public static class ScanFilterExtensions
{
    // This is a stupid extension method, but for whatever unkown reason the AWS SDK has defined add methods that
    // return void, rather than returning "this" which means we can't chain them in a progressive set of calls,
    // so I've added this method to allow me to do this.
    // god damn it AWS, why, it's normal for a builder pattern god damn you
    public static ScanFilter ChainCondition(this ScanFilter filter, string name, ScanOperator op, params DynamoDBEntry[] values)
    {
        filter.AddCondition(name, op, values);
        return filter;
    }
}