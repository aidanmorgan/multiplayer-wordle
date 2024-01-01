namespace Wordle.Redis.Common;

public class RedisSettings
{
    public string RedisHost { get; set; }
    public string RedisTopic { get; set; }
    
    public string InstanceType { get; set; }
    public string InstanceId { get; set; }
}