using System.Collections.Concurrent;
using Apache.NMS;
using Apache.NMS.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Polly;
using Wordle.ActiveMq.Common;
using Wordle.Clock;
using Wordle.Common;
using Wordle.Events;
using IStartable = Autofac.IStartable;

namespace Wordle.ActiveMq.Publisher;

public class ActiveMqPublisherService : IEventPublisherService
{
    private readonly ActiveMqEventPublisherSettings _settings;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    
    public ActiveMqPublisherService(ActiveMqEventPublisherSettings settings, IClock clock, ILogger<ActiveMqPublisherService> logger)
    {
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }

    private IDictionary<Type, BlockingCollection<IEvent>> _collections =
        new Dictionary<Type, BlockingCollection<IEvent>>();
    
    public async Task RunAsync(CancellationToken token)
    {
        // set up the shared collections that are used by the producers, these are always appended to
        // by the publish method.
        foreach (var type in EventUtil.GetAllEventTypes())
        {
            var collection = new BlockingCollection<IEvent>();
            _collections[type] = collection;
        }

        var context = new Context().Initialise(_logger);
        var result = await _settings.ServicePolicy.ExecuteAndCaptureAsync(async (context) =>
        {
            var serviceCancel = new CancellationTokenSource();

            IConnectionFactory factory = new NMSConnectionFactory(_settings.ActiveMqUri);
            var connection = await factory.CreateConnectionAsync("artemis", "artemis");
            await connection.StartAsync();
            
            var session = await connection.CreateSessionAsync();

            var tasks = new List<Task>();

            foreach (var type in EventUtil.GetAllEventTypes())
            {
                var topicName = $"topic://VirtualTopic.{ActiveMqSettings.TopicNamer(type)}";
                var destination = (ITopic)SessionUtil.GetDestination(session, topicName);
                
                _logger.LogInformation("Creating producer for Event topic {TopicName}", topicName);
                
                tasks.Add(Task.Run(async () =>
                {
                    var producer = await session.CreateProducerAsync(destination);
                    await Producer(type, producer, session, serviceCancel.Token);
                }, serviceCancel.Token));
            }

            await Task.WhenAny(tasks);

            if (tasks.Any(x => x.IsFaulted))
            {
                try
                {
                    // we haven't been asked to shut the service down, so we need to kill off the background threads
                    // and then re-start them again
                    if (!token.IsCancellationRequested)
                    {
                        await serviceCancel.CancelAsync();
                        Task.WaitAll(tasks.ToArray(), _settings.ProducerThreadCancelWait);

                        throw new AggregateException(tasks
                            .Where(x => x.IsFaulted)
                            .Select(x => x.Exception)!);
                    }
                }
                finally
                {
                    await Cleanup(connection, session);
                }
            }
        }, token);

    }

    private async Task Producer(Type type, IMessageProducer producer, ISession session, CancellationToken serviceCancel)
    {
        var ctx = new Context().Initialise(_logger);
        
        var result = await _settings.ProducerPolicy.ExecuteAndCaptureAsync(async (ctx) =>
        {
            try
            {
                while (!serviceCancel.IsCancellationRequested)
                {
                    var ev = _collections[type].Take(serviceCancel);
                    // this check makes sure that the event we're generating hasn't already been dispatched somewhere
                    // else as these values are only set by the publisher
                    if (!string.IsNullOrEmpty(ev.EventSourceId) || !string.IsNullOrEmpty(ev.EventSourceType))
                    {
                        return;
                    }

                    ev.EventSourceType = _settings.InstanceType;
                    ev.EventSourceId = _settings.InstanceId;
                    ev.Timestamp = _clock.UtcNow();

                    var payload = JsonConvert.SerializeObject(ev);

                    var message = await session.CreateTextMessageAsync(payload);
                    // this attempts to keep all messages of the same type going to the same consumer, so it makes sense to keep
                    // all messages related to a tenant going to the same place if possible - that way we can introduce some
                    // "borderline" concept of event ordering - /shrug
                    // TODO : this needs A LOT more testing
                    message.Properties["JMSXGroupID"] = ev.Tenant;

                    await producer.SendAsync(message, MsgDeliveryMode.Persistent, MsgPriority.Normal,
                        _settings.EventTimeToLive);

                    _logger.LogInformation("Published: {Payload} via ActiveMQ", payload);
                }
            }
            // suppressing this on purpose, if it is thrown it is because the token has been cancelled from outside of this
            // thread and we are just going to shut down anyway.
            catch(OperationCanceledException) {}
        }, serviceCancel);

        try
        {
            await producer.CloseAsync();
        }
        // just done for safety, okay to ignore
        catch(Exception) { }

        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogWarning(result.FinalException, "Producer for event {EventType} has exceeded retry attempts", type);
            throw new AggregateException(result.FinalException);
        }    
    }

    private async Task Cleanup(IConnection connection, ISession session)
    {
        // the consumer thread has met it's retry limit, so we are going to need to clean up the resources
        // that are shared between all of the background threads.
        try
        {
            await session.CloseAsync();
        }
        finally
        {
            session?.Dispose();
        }
        
        try
        {
            await connection.CloseAsync();
        }
        finally
        {
            connection?.Dispose();
        }
    }
 

    public async Task Publish(IEvent ev, CancellationToken token)
    {
        // we dont publish immediately, we add the event to a blocking queue that is used by a background thread
        // that actually does the publishing.
        _collections[ev.GetType()].Add(ev, token);
    }
}