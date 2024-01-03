using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Wordle.Redis.Common;

public class RedisRetryPolicy
{
    private static readonly Func<int, TimeSpan> RetryAttemptWaitProvider =
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

    public static AsyncRetryPolicy CreateRetryPolicy(int retryCount, Func<int, TimeSpan>? waitProvider = null)
    {
        return Policy
            .Handle<RedisServerException>()
            .Or<RedisConnectionException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(retryCount, waitProvider ?? RetryAttemptWaitProvider);
    }
}