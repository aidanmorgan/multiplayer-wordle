using Apache.NMS;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Wordle.ActiveMq.Common;

namespace Wordle.ActiveMq.Publisher;

public class ActiveMqEventPublisherSettings : ActiveMqSettings
{
    public string InstanceType { get; init; }
    public string InstanceId { get; init; }
    public string ActiveMqUri { get; init; } = "activemq:tcp://localhost:61616";
    public TimeSpan EventTimeToLive { get; init; } = TimeSpan.FromHours(2);

    public int ServiceRetryCount { get; init; } = 5;
    public TimeSpan ServiceRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public AsyncRetryPolicy ServicePolicy =>
        Policy.Handle<NMSException>()
            .WaitAndRetryAsync(ServiceRetryCount, (x) => ServiceRetryDelay,             
            onRetry: (x, i, c) =>
            {
                (c.GetLogger()).LogWarning(x, $"S{nameof(ActiveMqPublisherService)} Service error...");
            });

    public int ProducerRetryCount { get; init; } = 0;
    public TimeSpan ProducerRetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    
    public AsyncRetryPolicy ProducerPolicy =>
    Policy.Handle<NMSException>()
        .WaitAndRetryAsync(ProducerRetryCount, (x) => ProducerRetryDelay,             
            onRetry: (x, i, c) =>
            {
                (c.GetLogger()).LogWarning(x, $"{nameof(ActiveMqPublisherService)} Producer error...");
            });

    
    public TimeSpan ProducerThreadCancelWait { get; init; } = TimeSpan.FromSeconds(5);
}