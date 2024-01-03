using Apache.NMS;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Wordle.ActiveMq.Common;
using Wordle.Events;

namespace Wordle.ActiveMq.Consumer;

public class ActiveMqEventConsumerSettings : ActiveMqSettings
{
    public string InstanceType { get; init; }
    
    public string InstanceId { get; init; }
    
    public string ActiveMqUri { get; init; } = "activemq:tcp://localhost:61616";

    public int ServiceRetryCount { get; init; } = 10;
    // how long to wait before attempting to reconnect the service back to the 
    public TimeSpan ServiceRetryDelay { get; init; } = TimeSpan.FromSeconds(5);


    public int ConsumerRetryCount { get; init; } = 0;
    public TimeSpan ConsumerRetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    
    public AsyncRetryPolicy ServiceRetryPolicy =>  Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(ServiceRetryCount, (x) => ServiceRetryDelay, 
            onRetry: (x, i, c) =>
            {
                (c.GetLogger()).LogWarning(x, $"{nameof(ActiveMqEventConsumerService)}Service error...");
            });

    public AsyncRetryPolicy ConsumerRetryPolicy =>  Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(ConsumerRetryCount, (x) => ConsumerRetryDelay, 
            onRetry: (x, i, c) =>
            {
                (c.GetLogger()).LogWarning(x, $"{nameof(ActiveMqEventConsumerService)} Consumer error...");
            });    
    
    // how long to block the processing thread while waiting for a message before allowing a cancellation token
    // check to occur. Should be set to the value of ConsumerThreadCancelWait / 2 ideally to ensure that we are checking
    // the token at a reasonable frequency.
    public TimeSpan ConsumerPollTimeout { get; init; } = TimeSpan.FromSeconds(1);
    
    // When a consumer background thread fails, how long to wait for the other threads to cancel before just continuing
    // on the process.
    public TimeSpan ConsumerThreadCancelWait { get; set; } = TimeSpan.FromSeconds(5);

    // the 
    public List<Type> EventTypesToMonitor { get; set; } = EventUtil.GetAllEventTypes();

}