using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Nito.AsyncEx;
using Wordle.Common;
using Wordle.Events;
using Apache.NMS;
using Apache.NMS.Util;
using Polly;
using Wordle.ActiveMq.Common;
using IStartable = Autofac.IStartable;

namespace Wordle.ActiveMq.Consumer;

public class ActiveMqEventConsumerService : IEventConsumerService
{
    private readonly ActiveMqEventConsumerSettings _settings;
    private readonly IMediator _mediator;
    private readonly ILogger<ActiveMqEventConsumerService> _logger;

    public ActiveMqEventConsumerService(ActiveMqEventConsumerSettings settings, IMediator mediator, ILogger<ActiveMqEventConsumerService> logger)
    {
        _settings = settings;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var activeMqSafeInstanceType = new string(_settings.InstanceType.Where(char.IsLetterOrDigit).ToArray());
        var ctx = new Context().Initialise(_logger);

        var consumerCancellationToken = new CancellationTokenSource();
        var result = await _settings.ServiceRetryPolicy.ExecuteAndCaptureAsync(async (ctx) =>
        {
            IConnectionFactory factory = new NMSConnectionFactory(_settings.ActiveMqUri);
            var connection = await factory.CreateConnectionAsync();
            await connection.StartAsync();

            var session = await connection.CreateSessionAsync();

            var tasks = new List<Task>();
            
            // TODO : this approach may be too complex, we are using a topic per event on the publisher side but that means
            // that we now have to receive all of the events we are interested in on different consumers, which has a few interesting
            // quirks i am attempting to address in this code.
            // 1. if a failure of one event consumer occurs we (at the moment) force shut-down all of the consumers and consider it
            //    a failure of the entire system
            // 2. we may introduce some "interesting" sequencing problems if we have events for a specific session that have an implied
            //    order in them. I am working around this concept by attempting to use ActiveMQ's message grouping feature to 
            //    try and keep all events that are for a specific session coming to the same consumer service, but there is still
            //    the potential for events to be seen by game logic as coming through in a nonsensical order.
            //
            // This does create some _interesting_ issues when it comes to sequencing the threads below.
            foreach (var eventClass in _settings.EventTypesToMonitor)
            {
                var eventKey = ActiveMqSettings.TopicNamer(eventClass);
                var topicName = $"queue://Consumer.{activeMqSafeInstanceType}.VirtualTopic.{eventKey}";
                var queue = SessionUtil.GetDestination(session, topicName);
                
                tasks.Add(Task.Run(async () =>
                {                
                    var consumer = await session.CreateConsumerAsync(queue);
                    _logger.LogInformation("Creating shared durable consumer for Topic {TopicName}", topicName);
                    await EventConsumer(eventClass, consumer, consumerCancellationToken.Token);
                }, token));
            }
            
            // we are going to block at this point until one of the child threads we just spawned comes back with a problem
            // to consider
            await Task.WhenAny(tasks);

            if (tasks.Any(x => x.IsFaulted))
            {
                try
                {
                    // if we are cleaning up because the application has requested to be shutdown then that is okay
                    // but if we are in this step because at least one of the consumer threads has died then we need
                    // to cancel all of the other threads and then clean up the resources as we're going to try and
                    // reconnect at the top-level.
                    if (!token.IsCancellationRequested)
                    {
                        await consumerCancellationToken.CancelAsync();
                        Task.WaitAll(tasks.ToArray(), _settings.ConsumerThreadCancelWait);

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

        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogCritical(result.FinalException, "{Name} Failure...", GetType().Name);
        }
        else
        {
            _logger.LogInformation("{Name} exiting cleanly...", GetType().Name);
        }
    }
    
    private async Task EventConsumer(Type eventType, IMessageConsumer consumer, CancellationToken token)
    {
        var ctx = new Context().Initialise(_logger);
        var result = await _settings.ConsumerRetryPolicy.ExecuteAndCaptureAsync(async (ctx) =>
        {
            while (!token.IsCancellationRequested)
            {
                // activemq for whatever reason doesn't support cancellation tokens, so what we need to do here is a
                // relatively "short" poll to see if there is any data - if there isn't then we need to spin back around
                // and check the cancellation token to see if this thread needs to shut down.
                var message = await consumer.ReceiveAsync(_settings.ConsumerPollTimeout) as ITextMessage;
                if (message == null || string.IsNullOrEmpty(message.Text))
                {
                    continue;
                }

                var @event = (IEvent)JsonSerializer.Deserialize(message.Text, eventType);

                _logger.LogInformation("Received Event: {Event}", message.Text);
                await _mediator.Publish(@event);
            }
        }, token);

        try
        {
            await consumer.CloseAsync();
        }
        // just done for safety, okay to ignore
        catch(Exception) { }

        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogWarning(result.FinalException, "Consumer for event {EventType} has exceeded retry attempts", eventType);
            throw new AggregateException(result.FinalException);
        }
    }
    
    private static async Task Cleanup(IConnection connection, ISession session)
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

}