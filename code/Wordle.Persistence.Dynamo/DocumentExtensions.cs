using Amazon.DynamoDBv2.DocumentModel;

namespace Wordle.Persistence.Dynamo;

public static class DocumentExtensions
{
    public static bool IsSession(this Document x)
    {
        return x["sk"].AsString().StartsWith("session");
    }
    
    public static bool IsTenant(this Document x)
    {
        return x["sk"].AsString().StartsWith("tenant");
    }    

    public static bool IsRound(this Document x)
    {
        return x["sk"].AsString().StartsWith("round");
    }

    public static bool IsOptions(this Document x)
    {
        return x["sk"].AsString().StartsWith("options");
    }

    public static bool IsGuess(this Document x)
    {
        return x["sk"].AsString().StartsWith("guess");
    }
}