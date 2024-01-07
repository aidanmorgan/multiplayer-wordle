using Apache.NMS;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Wordle.ActiveMq.Common;

namespace Wordle.ActiveMq.Publisher;

public class ActiveMqEventPublisherOptions : ActiveMqOptions
{
    public string InstanceType { get; init; }
    public string InstanceId { get; init; }
    public string ActiveMqUri { get; init; }
    
    public ManualResetEventSlim ReadySignal { get; init; } = new ManualResetEventSlim(false);

    public TimeSpan EventTimeToLive { get; init; } = TimeSpan.FromHours(2);

    public TimeSpan ServiceRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaximumServiceRetryTime { get; init; } = TimeSpan.FromMinutes(10);

    public int ServiceRetryCount =>
        (int)Math.Floor(MaximumServiceRetryTime.TotalMilliseconds / ServiceRetryDelay.TotalMilliseconds);

    public AsyncRetryPolicy ServicePolicy =>
        Policy.Handle<NMSException>()
            .WaitAndRetryAsync(ServiceRetryCount, (x) => ServiceRetryDelay,             
            onRetry: (x, i, c) =>
            {
                (c.GetLogger()).LogWarning(x, $"S{nameof(ActiveMqEventPublisherService)} Service error...");
            });

    public TimeSpan ProducerRetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaximumProducerRetryTime { get; init; } = TimeSpan.FromSeconds(10);
    public int ProducerRetryCount => (int)Math.Floor(MaximumProducerRetryTime.TotalMilliseconds / ProducerRetryDelay.TotalMilliseconds);
    
    public AsyncRetryPolicy ProducerPolicy =>
    Policy.Handle<NMSException>()
        .WaitAndRetryAsync(ProducerRetryCount, (x) => ProducerRetryDelay,             
            onRetry: (x, i, c) =>
            {
                (c.GetLogger()).LogWarning(x, $"{nameof(ActiveMqEventPublisherService)} Producer error...");
            });

    
    public TimeSpan ProducerThreadCancelWait { get; init; } = TimeSpan.FromSeconds(5);
}