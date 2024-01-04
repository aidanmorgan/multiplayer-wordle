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

using static Wordle.ActiveMq.Common.ActiveMqUtil;

namespace Wordle.ActiveMq.Publisher;

public class ActiveMqEventPublisherService : IEventPublisherService
{
    private readonly ActiveMqEventPublisherOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<ActiveMqEventPublisherService> _logger;
    
    public ManualResetEventSlim ReadySignal => _options.ReadySignal;
    
    public ActiveMqEventPublisherService(ActiveMqEventPublisherOptions options, IClock clock, ILogger<ActiveMqEventPublisherService> logger)
    {
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    private readonly IDictionary<Type, BlockingCollection<IEvent>> _publisherQueues =
        new Dictionary<Type, BlockingCollection<IEvent>>();
    
    public async Task RunAsync(CancellationToken token)
    {
        // set up the shared collections that are used by the producers, these are always appended to
        // by the publish method.
        foreach (var type in EventUtil.GetAllEventTypes())
        {
            var collection = new BlockingCollection<IEvent>();
            _publisherQueues[type] = collection;
        }

        var context = new Context().Initialise(_logger);
        var result = await _options.ServicePolicy.ExecuteAndCaptureAsync(async (c,ct) =>
        {
            var serviceCancel = new CancellationTokenSource();
            
            IConnectionFactory factory = new NMSConnectionFactory(_options.ActiveMqUri);

            var tasks = new List<Task>();

            foreach (var type in EventUtil.GetAllEventTypes())
            {
                tasks.Add(Task.Run(async () =>
                {
                    IConnection connection = null;
                    ISession session = null;
                    IMessageProducer producer = null;
                    
                    try
                    {
                        connection = await factory.CreateConnectionAsync("artemis", "artemis");
                        await connection.StartAsync();

                        session = await connection.CreateSessionAsync();

                        var topicName = $"topic://VirtualTopic.{ActiveMqOptions.TopicNamer(type)}";
                        var destination = (ITopic)SessionUtil.GetDestination(session, topicName);

                        _logger.LogInformation("Creating producer for Event topic {TopicName}", topicName);


                        producer = await session.CreateProducerAsync(destination);
                        await Producer(type, producer, session, serviceCancel.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        await CloseQuietly(producer, _logger);
                        await CloseQuietly(session, _logger);
                        await CloseQuietly(connection, _logger);
                    }
                }, serviceCancel.Token));
            }

            try
            {
                _options.ReadySignal.Set();
                _logger.LogInformation($"{nameof(ActiveMqEventPublisherService)} has started and is ready to publish events...");
                Task.WaitAny(tasks.ToArray(), ct);
            }
            catch (OperationCanceledException) { }

            if (tasks.Any(x => x.IsFaulted))
            {
                    // we haven't been asked to shut the service down, so we need to kill off the background threads
                    // and then re-start them again.
                    //
                    // this is intetnionally not using the service token as that isn't at this level
                    if (!ct.IsCancellationRequested)
                    {
                        await serviceCancel.CancelAsync();
                        Task.WaitAll(tasks.ToArray(), _options.ProducerThreadCancelWait);

                        throw new AggregateException(tasks
                            .Where(x => x.IsFaulted)
                            .Select(x => x.Exception)!);
                    }
            }
        }, context, token);

        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogCritical(result.FinalException, $"{nameof(ActiveMqEventPublisherService)} exiting with failure");
        }
        else
        {
            _logger.LogInformation($"{nameof(ActiveMqEventPublisherService)} exiting.");
        }
    }

    private async Task Producer(Type type, IMessageProducer producer, ISession session, CancellationToken serviceCancel)
    {
        var ctx = new Context().Initialise(_logger);
        var result = await _options.ProducerPolicy.ExecuteAndCaptureAsync(async (c, ct) =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var ev = _publisherQueues[type].Take(ct);
                    // this check makes sure that the event we're generating hasn't already been dispatched somewhere
                    // else as these values are only set by the publisher
                    if (!string.IsNullOrEmpty(ev.EventSourceId) || !string.IsNullOrEmpty(ev.EventSourceType))
                    {
                        continue;
                    }

                    if (ev.EventSourceType == _options.InstanceType)
                    {
                        continue;
                    }

                    ev.EventSourceType = _options.InstanceType;
                    ev.EventSourceId = _options.InstanceId;
                    ev.Timestamp = _clock.UtcNow();

                    var payload = JsonConvert.SerializeObject(ev);

                    var message = await session.CreateTextMessageAsync(payload);
                    // this attempts to keep all messages of the same type going to the same consumer, so it makes sense to keep
                    // all messages related to a tenant going to the same place if possible - that way we can introduce some
                    // "borderline" concept of event ordering - /shrug
                    // TODO : this needs A LOT more testing
                    message.Properties["JMSXGroupID"] = ev.Tenant;

                    await producer.SendAsync(message, MsgDeliveryMode.Persistent, MsgPriority.Normal,
                        _options.EventTimeToLive);

                    _logger.LogInformation("Published: {Payload} via ActiveMQ", payload);
                }
            }
            // suppressing this on purpose, if it is thrown it is because the token has been cancelled from outside of this
            // thread and we are just going to shut down anyway.
            catch(OperationCanceledException) {}
        }, ctx, serviceCancel);

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
        _publisherQueues[ev.GetType()].Add(ev, token);
    }
}