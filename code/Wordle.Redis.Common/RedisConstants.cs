namespace Wordle.Redis.Common;

public static class RedisConstants
{
    public const string EventTypeKey = "event-type";
    public const string EventIdKey = "event-id";
    public const string EventSourceTypeKey = "event-source-type";
    public const string EventSourceIdKey = "event-source-id";
    public const string PayloadKey = "event-payload";
    
    
    static RedisConstants()
    {
        
    }
}