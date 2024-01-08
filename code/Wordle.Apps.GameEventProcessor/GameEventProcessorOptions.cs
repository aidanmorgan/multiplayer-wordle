using Apache.NMS;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Wordle.ActiveMq.Common;
using Wordle.Apps.GameEventProcessor.Impl;

namespace Wordle.Apps.GameEventProcessor;

public class GameEventProcessorOptions
{
    public TimeSpan MaxCleanShutdownWait { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaximumStartTimeout { get; set; } = TimeSpan.FromSeconds(20);

    public TimeSpan MaximumSessionAge { get; set; } = TimeSpan.FromHours(6);

    public TimeSpan PreiodicCleanupFrequency { get; set; } = TimeSpan.FromHours(1);

    public string ActiveMqUri { get; init; }
    public string TaskQueueName { get; init; } = "DelayProcessing";

    public string InstanceType { get; init; }

    public string InstanceId { get; init; }

    // signalled by the service to indicate that it has finished initialising and is ready to process messages
    public ManualResetEventSlim ReadySignal { get; init; } = new ManualResetEventSlim(false);


    public TimeSpan PublishRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaximumPublishRetryTime { get; init; } = TimeSpan.FromMinutes(10);

    public int PublishRetryCount =>
        (int)Math.Floor(MaximumPublishRetryTime.TotalMilliseconds / PublishRetryDelay.TotalMilliseconds);

    public AsyncRetryPolicy PublishRetryPolicy => Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(PublishRetryCount, (x) => PublishRetryDelay,
            onRetry: (e, t, i, c) =>
            {
                c.GetLogger().LogWarning(e, $"{nameof(ActiveMqDelayProcessingService)} Publisher Exception");
            });

    public TimeSpan ConsumerRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaximumConsumerRetryTime { get; init; } = TimeSpan.FromMinutes(10);

    public int ConsumerRetryCount =>
        (int)Math.Floor(MaximumConsumerRetryTime.TotalMilliseconds / ConsumerRetryDelay.TotalMilliseconds);

    public AsyncRetryPolicy ConsumerRetryPolicy => Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(PublishRetryCount, (x) => PublishRetryDelay,
            onRetry: (e, t, i, c) =>
            {
                c.GetLogger().LogWarning(e, $"{nameof(ActiveMqDelayProcessingService)} Publisher Exception");
            });

    public TimeSpan ConsumeReceiveTimeout { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan CleanShutdownDelay { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public Func<Guid, string> SessionLockKey { get; set; }

    public GameEventProcessorOptions()
    {
        SessionLockKey = (x) => $"Session:{x}";
    }
}