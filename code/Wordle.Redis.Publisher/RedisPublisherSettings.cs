using StackExchange.Redis;
using Wordle.Redis.Common;

namespace Wordle.Redis.Publisher;

public class RedisPublisherSettings : RedisSettings
{
    // the unique key to use in redis for the sorted set that tracks seen events
    public RedisKey SeenEventIdKey => new($"{InstanceType}-seen-events");

    // how far into the past we should keep events for
    public TimeSpan SeenEventExpiry { get; set; } = TimeSpan.FromMinutes(5);
    
    // the timestamp to use for the start of the seen event checks, anything older than this
    // time is automatically removed.
    //
    // that is, events that occur before this value
    public DateTimeOffset SeenEventWindowStart { get; set; } = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(1));
    
    // how frequently the background task should perform the cleanup
    public TimeSpan SeenEventInterval { get; set; } = TimeSpan.FromMinutes(5);
    
    public int MaxPublishRetries { get; set; } = 5;
}