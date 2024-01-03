using Wordle.Redis.Common;

namespace Wordle.Redis.Consumer;

public class RedisConsumerSettings : RedisSettings
{
    public int MaxConsumeRetries { get; set; } = 5;

    public TimeSpan MaximumEventAge { get; set; } = TimeSpan.FromHours(2);
}