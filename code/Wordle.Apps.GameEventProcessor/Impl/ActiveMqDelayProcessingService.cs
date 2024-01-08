using System.Collections.Concurrent;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Medallion.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Wordle.ActiveMq.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Queries;
using static Wordle.ActiveMq.Common.ActiveMqUtil;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class ActiveMqDelayProcessingService : IDelayProcessingService
{
    private readonly ActiveMqDelayProcessingOptions _options;
    private readonly ILogger<ActiveMqDelayProcessingService> _logger;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IClock _clock;
    private readonly IMediator _mediator;

    private readonly BlockingCollection<TimeoutPayload> _publishQueue = new BlockingCollection<TimeoutPayload>();

    public ManualResetEventSlim ReadySignal => _options.ReadySignal;

    public ActiveMqDelayProcessingService(ActiveMqDelayProcessingOptions options, IMediator mediator, IDistributedLockProvider lockProvider, IClock clock, ILogger<ActiveMqDelayProcessingService> logger)
    {
        _options = options;
        _mediator = mediator;
        _lockProvider = lockProvider;
        _clock = clock;
        _logger = logger;
    }

    public Task ScheduleRoundUpdate(VersionId session, VersionId round, DateTimeOffset executionTime, CancellationToken token)
    {
        _publishQueue.Add(new TimeoutPayload(session.Id, session.Version, round.Id, round.Version, executionTime), token);
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var cleanInstanceType = new string(_options.InstanceType.Where(char.IsLetterOrDigit).ToArray());
        var serviceCancellation = new CancellationTokenSource();
        
        _logger.LogInformation("Starting {ServiceName} connecting to {ActiveMqUri}", nameof(ActiveMqDelayProcessingService), _options.ActiveMqUri);
        
        var factory = new ConnectionFactory(_options.ActiveMqUri);

        var all = new List<Task<PolicyResult>>()
        {
            Task.Run(async () => await RunPublisherAsync(factory, serviceCancellation), serviceCancellation.Token),
            Task.Run(async () => await RunConsumerAsync(cleanInstanceType, factory, serviceCancellation), serviceCancellation.Token)
        };

        try
        {
            _options.ReadySignal.Set();
            Task.WaitAny(all.ToArray(), token);
        }catch(OperationCanceledException) { }

        _logger.LogInformation($"Commencing shutdown of {nameof(ActiveMqDelayProcessingService)}");
        
        if (!token.IsCancellationRequested)
        {
            await serviceCancellation.CancelAsync();
            Task.WaitAll(all.Where(x => !x.IsFaulted).ToArray(), _options.CleanShutdownDelay);
        }

        // now go through and try and work out why we're shutting down so we can log it
        var results = all.Select(x => x.Result).ToList();

        if (results.Any(x => x.Outcome == OutcomeType.Failure))
        {
            results.Where(x => x.Outcome == OutcomeType.Failure)
                .Select(x => x.FinalException)
                .ToList()
                .ForEach(x =>
                {
                    _logger.LogCritical(x, $"Exiting {nameof(ActiveMqDelayProcessingService)} due to Exception.");
                });
        }
        else
        {
            _logger.LogInformation($"Existing {nameof(ActiveMqDelayProcessingService)} cleanly.");
        }
    }
    
    private Task<PolicyResult> RunConsumerAsync(string cleanInstanceType, IConnectionFactory factory, CancellationTokenSource serviceCancellation)
    {
        var consumeContext = new Context().Initialise(_logger);
        var consumeResult = _options.ConsumerRetryPolicy.ExecuteAndCaptureAsync(async (c, ct) =>
        {
            var connection = await factory.CreateConnectionAsync();
            await connection.StartAsync();

            var session = await connection.CreateSessionAsync(AcknowledgementMode.ClientAcknowledge);
            
            var queueName = $"Consumer.{cleanInstanceType}.VirtualTopic.{_options.TaskQueueName}";
            var queue = await session.GetQueueAsync(queueName);
            
            var consumer = await session.CreateConsumerAsync(queue);
            
            _logger.LogInformation("Job scheduler consuming from {QueueName}", queueName);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var message = await consumer.ReceiveAsync(_options.ConsumeReceiveTimeout) as ITextMessage;
                    if (message == null)
                    {
                        continue;
                    }

                    try
                    {
                        var @event = JsonConvert.DeserializeObject<TimeoutPayload>(message.Text);
                        await HandleTimeout(@event);
                    }
                    finally
                    {
                        await message.AcknowledgeAsync();
                    }
                }
            }
            finally
            {
                await CloseQuietly(consumer, _logger);
                await CloseQuietly(session, _logger);
                await CloseQuietly(connection, _logger);
            }
        }, consumeContext, serviceCancellation.Token);
        return consumeResult;
    }

    private Task<PolicyResult> RunPublisherAsync(IConnectionFactory factory, CancellationTokenSource serviceCancellation)
    {
        var publishContext = new Context().Initialise(_logger);
        
        var publisherResult = _options.PublishRetryPolicy.ExecuteAndCaptureAsync(async (c, ct) =>
        {
            var connection = await factory.CreateConnectionAsync();
            await connection.StartAsync();

            var session = await connection.CreateSessionAsync();
            var topicName = $"VirtualTopic.{_options.TaskQueueName}";
            
            var topicDestination = await session.GetTopicAsync(topicName);
            var producer = await session.CreateProducerAsync(topicDestination);

            _logger.LogInformation("Job scheduler publishing to {TopicName}", topicName);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var value = _publishQueue.Take(ct);

                    var delayMillis = (int)Math.Max(Math.Ceiling(value.Timeout.Subtract(_clock.UtcNow()).TotalMilliseconds), 0);

                    var message = await session.CreateTextMessageAsync(JsonConvert.SerializeObject(value));
                    if (delayMillis > 0)
                    {
                        message.Properties["AMQ_SCHEDULED_DELAY"] = delayMillis;
                        message.Properties["_AMQ_SCHED_DELIVERY"] = delayMillis;

                        _logger.LogInformation(
                            "Check for Session {SessionId} in {DelaySeconds} seconds (at: {JobTime})", value.SessionId,
                            (delayMillis / 1000), value.Timeout);
                    }
                    else
                    {
                        _logger.LogInformation("Immediate check for Session {SessionId} submitted", value.SessionId);
                    }

                    await producer.SendAsync(message);
                }
            }
            finally
            {
                await CloseQuietly(producer, _logger);
                await CloseQuietly(session, _logger);
                await CloseQuietly(connection, _logger);
            }

        }, publishContext, serviceCancellation.Token);
        return publisherResult;
    }

    public async Task HandleTimeout(TimeoutPayload payload)
    {
        await using var dLock = await _lockProvider.AcquireLockAsync($"Session:{payload.SessionId}", _options.LockTimeout);
        try
        {
            await _mediator.Send(new EndActiveRoundCommand(payload.SessionId, payload.SessionVersion,
                payload.RoundId, payload.RoundVersion));
        }
        catch (EndActiveRoundCommandException x)
        {
            _logger.LogError("Attempt to end Round {RoundId} for Session {SessionId} failed with message {Message}",
                payload.RoundId, payload.SessionId, x.Message);
        }
    }
}
