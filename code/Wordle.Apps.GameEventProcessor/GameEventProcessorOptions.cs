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

    
    /// <summary>
    /// How long we should wait before attempting to reconnect the conumer.
    /// </summary>
    public TimeSpan ConsumerRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// How long (maximium) we are willing to wait for the consumer reconnect process to succeed before aborting.
    /// </summary>
    public TimeSpan MaximumConsumerRetryTime { get; init; } = TimeSpan.FromMinutes(10);

    public int ConsumerRetryCount =>
        (int)Math.Floor(MaximumConsumerRetryTime.TotalMilliseconds / ConsumerRetryDelay.TotalMilliseconds);

    public AsyncRetryPolicy ConsumerRetryPolicy => Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(ConsumerRetryCount, (x) => PublishRetryDelay,
            onRetry: (e, t, i, c) =>
            {
                c.GetLogger().LogWarning(e, $"{nameof(ActiveMqDelayProcessingService)} Publisher Exception");
            });

    
    /// <summary>
    /// How long we will wait for the consume from the ActiveMq consumer before looping around and checking the cancellation
    /// token again.
    /// </summary>
    public TimeSpan ConsumeReceiveTimeout { get; init; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// How long we are willing to wait for a clean shutdown to complete before forcing the shutdown to occur.
    /// </summary>
    public TimeSpan CleanShutdownDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long we should wait to obtain the distributed lock for updating a Session.
    /// </summary>
    public TimeSpan DistributedLockTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Used to create a distributed lock key given a session id.
    /// </summary>
    public Func<Guid, string> SessionLockKey { get; set; }

    public GameEventProcessorOptions()
    {
        SessionLockKey = (x) => $"Session:{x}";
    }
}