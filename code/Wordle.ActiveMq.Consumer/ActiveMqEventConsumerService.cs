using System.Collections.Concurrent;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Wordle.Common;
using Wordle.Events;
using Apache.NMS;
using Apache.NMS.Util;
using Polly;
using Wordle.ActiveMq.Common;

using static Wordle.ActiveMq.Common.ActiveMqUtil;

namespace Wordle.ActiveMq.Consumer;

public class ActiveMqEventConsumerService : IEventConsumerService
{
    private readonly ActiveMqEventConsumerOptions _options;
    private readonly IMediator _mediator;
    private readonly ILogger<ActiveMqEventConsumerService> _logger;

    public ManualResetEventSlim ReadySignal => _options.ReadySignal;
   

    public ActiveMqEventConsumerService(ActiveMqEventConsumerOptions options, IMediator mediator, ILogger<ActiveMqEventConsumerService> logger)
    {
        _options = options;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var activeMqSafeInstanceType = new string(_options.InstanceType.Where(char.IsLetterOrDigit).ToArray());
        var ctx = new Context().Initialise(_logger);

        var consumerCancellationToken = new CancellationTokenSource();
        var result = await _options.ServiceRetryPolicy.ExecuteAndCaptureAsync(async (c,ct) =>
        {
            IConnectionFactory factory = new NMSConnectionFactory(_options.ActiveMqUri);

            var tasks = new List<Task>();
            var consumerReadySignals = new List<ManualResetEventSlim>();
            
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
            foreach (var eventClass in _options.EventTypesToMonitor)
            {
                tasks.Add(Task.Run(async () =>
                {
                    IConnection connection = null;
                    ISession session = null;
                    IMessageConsumer consumer = null;
                    
                    try 
                    {
                        connection = await factory.CreateConnectionAsync();
                        await connection.StartAsync();

                        session = await connection.CreateSessionAsync();

                        var eventKey = ActiveMqOptions.TopicNamer(eventClass);
                        var queueName = $"queue://Consumer.{activeMqSafeInstanceType}.VirtualTopic.{eventKey}";
                        var queue = SessionUtil.GetDestination(session, queueName);
                        
                        var signal = new ManualResetEventSlim();
                        consumerReadySignals.Add(signal);

                        consumer = await session.CreateConsumerAsync(queue);
                    
                        await RunConsumerAsync(queueName, eventClass, consumer, signal, consumerCancellationToken.Token);
                    }
                    finally
                    {
                        await CloseQuietly(consumer, _logger);
                        await CloseQuietly(session, _logger);
                        await CloseQuietly(connection, _logger);
                    }
                }, consumerCancellationToken.Token));
            }
            
            
            // we are going to block at this point until one of the child threads we just spawned comes back with a problem
            // to consider what we want to do next with
            try
            {
                CancellationTokenSource initialiseCancel = new CancellationTokenSource(_options.MaximumConsumerInitialiseTime);
                consumerReadySignals.AsParallel().ForAll(x =>
                { 
                    // this should throw an OperationCancelledException if the timeout threshold has passed which
                    // will automatically go into a shutdown process.
                    x.Wait(initialiseCancel.Token);
                });
                
                _options.ReadySignal.Set();
                _logger.LogInformation($"{nameof(ActiveMqEventConsumerService)} has started and is ready to receive events...");
                
                Task.WaitAny(tasks.ToArray(), ct);
            }catch(OperationCanceledException) { }

            if (tasks.Any(x => x.IsFaulted))
            {
                // if we are cleaning up because the application has requested to be shutdown then that is okay
                // but if we are in this step because at least one of the consumer threads has died then we need
                // to cancel all of the other threads and then clean up the resources as we're going to try and
                // reconnect at the top-level.
                if (!ct.IsCancellationRequested)
                {
                    await consumerCancellationToken.CancelAsync();
                    Task.WaitAll(tasks.ToArray(), _options.ConsumerThreadCancelWait);

                    throw new AggregateException(tasks
                        .Where(x => x.IsFaulted)
                        .Select(x => x.Exception)!);
                }
            }
        }, ctx, token);

        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogCritical(result.FinalException, "{Name} Failure...", GetType().Name);
        }
        else
        {
            _logger.LogInformation("{Name} exiting cleanly...", GetType().Name);
        }
    }
    
    private async Task RunConsumerAsync(string topicName, Type eventType, IMessageConsumer consumer, ManualResetEventSlim signal, CancellationToken token)
    {
        var ctx = new Context().Initialise(_logger);
        var result = await _options.ConsumerRetryPolicy.ExecuteAndCaptureAsync(async (c, ct) =>
        {
            _logger.LogInformation("Creating shared durable consumer for Topic {TopicName}", topicName);
            signal.Set();
            
            while (!ct.IsCancellationRequested)
            {
                // activemq for whatever reason doesn't support cancellation tokens, so what we need to do here is a
                // relatively "short" poll to see if there is any data - if there isn't then we need to spin back around
                // and check the cancellation token to see if this thread needs to shut down.
                var message = await consumer.ReceiveAsync(_options.ConsumerPollTimeout) as ITextMessage;
                if (message == null || string.IsNullOrEmpty(message.Text))
                {
                    continue;
                }

                var @event = (IEvent)JsonSerializer.Deserialize(message.Text, eventType);

                _logger.LogInformation("Received Event: {Event}", message.Text);
                await _mediator.Publish(@event);

//                await message.AcknowledgeAsync();
            }
        }, ctx, token);


        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogWarning(result.FinalException, "Consumer for event {EventType} has exceeded retry attempts", eventType);
            throw new AggregateException(result.FinalException);
        }
    }
}