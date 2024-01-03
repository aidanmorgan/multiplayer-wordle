namespace Wordle.Redis.Common;

public abstract class RedisSettings
{
    public string RedisHost { get; init; }
    public string RedisTopic { get; init; }
    
    public string InstanceType { get; init; }
    public string InstanceId { get; init; }
}