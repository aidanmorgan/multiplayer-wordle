using System.Collections.Concurrent;
using Apache.NMS;
using Apache.NMS.Util;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Wordle.ActiveMq.Common;
using Wordle.Clock;
using Wordle.Commands;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class ActiveMqDelayProcessingSettings
{
    public string ActiveMqUri { get; init; }
    public string TaskQueueName { get; init; } = "DelayProcessing";
    
    public string InstanceType { get; init; }
    
    public string InstanceId { get; init; }
    
    public TimeSpan PublishRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaximumPublishRetryTime { get; init; } = TimeSpan.FromMinutes(1);
    public int PublishRetryCount => (int)Math.Floor(MaximumPublishRetryTime.TotalMilliseconds / PublishRetryDelay.TotalMilliseconds);
    
    
    public AsyncRetryPolicy PublishRetryPolicy =>  Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(PublishRetryCount, (x) => PublishRetryDelay,
            onRetry: (e, t, i, c) =>
            {
                c.GetLogger().LogWarning(e, $"{nameof(ActiveMqDelayProcessingService)} Publisher Exception");
            });

    public TimeSpan ConsumerRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaximumConsumerRetryTime { get; init; } = TimeSpan.FromMinutes(1);
    public int ConsumerRetryCount => (int)Math.Floor(MaximumConsumerRetryTime.TotalMilliseconds / ConsumerRetryDelay.TotalMilliseconds);

    public AsyncRetryPolicy ConsumerRetryPolicy => Policy
        .Handle<NMSException>()
        .WaitAndRetryAsync(PublishRetryCount, (x) => PublishRetryDelay,
            onRetry: (e, t, i, c) =>
            {
                c.GetLogger().LogWarning(e, $"{nameof(ActiveMqDelayProcessingService)} Publisher Exception");
            });

    public TimeSpan ConsumeReceiveTimeout { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan CleanShutdownDelay { get; init; } = TimeSpan.FromSeconds(10);
}

public class ActiveMqDelayProcessingService : IDelayProcessingService
{
    private readonly ActiveMqDelayProcessingSettings _settings;
    private readonly ILogger<ActiveMqDelayProcessingService> _logger;
    private readonly IClock _clock;
    private readonly IMediator _mediator;

    private readonly BlockingCollection<TimeoutPayload> _publishQueue = new BlockingCollection<TimeoutPayload>();

    public ActiveMqDelayProcessingService(ActiveMqDelayProcessingSettings settings, IMediator mediator, IClock clock, ILogger<ActiveMqDelayProcessingService> logger)
    {
        _settings = settings;
        _mediator = mediator;
        _clock = clock;
        _logger = logger;
    }

    public Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken token)
    {
        _publishQueue.Add(new TimeoutPayload(sessionId, roundId, executionTime), token);
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var cleanInstanceType = new string(_settings.InstanceType.Where(char.IsLetterOrDigit).ToArray());
        var serviceCancellation = new CancellationTokenSource();
        
        var publishContext = new Context().Initialise(_logger);
        var publisherResult = _settings.PublishRetryPolicy.ExecuteAndCaptureAsync(action: async (c, ct) =>
        {
            IConnectionFactory factory = new NMSConnectionFactory(_settings.ActiveMqUri);
            var connection = await factory.CreateConnectionAsync();
            await connection.StartAsync();

            var session = await connection.CreateSessionAsync();
            var topicName = $"topic://VirtualTopic.{_settings.TaskQueueName}";
            var topicDestination = (ITopic)SessionUtil.GetDestination(session, topicName);
            var producer = await session.CreateProducerAsync(topicDestination);

            _logger.LogInformation("Job scheduler publishing to {TopicName}", topicName);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var value = _publishQueue.Take(ct);

                    var delayMillis =
                        (int)Math.Max(Math.Ceiling(value.Timeout.Subtract(_clock.UtcNow()).TotalMilliseconds), 0);

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
                await producer.CloseAsync();
                await session.CloseAsync();
                await connection.CloseAsync();
            }

        }, publishContext, serviceCancellation.Token);

        var consumeContext = new Context().Initialise(_logger);
        var consumeResult = _settings.ConsumerRetryPolicy.ExecuteAndCaptureAsync(async (c, ct) =>
        {
            IConnectionFactory factory = new NMSConnectionFactory(_settings.ActiveMqUri);
            var connection = await factory.CreateConnectionAsync();
            await connection.StartAsync();

            var session = await connection.CreateSessionAsync();
            
            var queueName = $"queue://Consumer.{cleanInstanceType}.VirtualTopic.{_settings.TaskQueueName}";
            var queue = (IQueue)SessionUtil.GetDestination(session, queueName);
            
            var consumer = await session.CreateConsumerAsync(queue);
            
            _logger.LogInformation("Job scheduler consuming from {QueueName}", queueName);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var message = await consumer.ReceiveAsync(_settings.ConsumeReceiveTimeout) as ITextMessage;

                    if (message == null || string.IsNullOrEmpty(message.Text))
                    {
                        continue;
                    }

                    var @event = JsonConvert.DeserializeObject<TimeoutPayload>(message.Text);
                    await HandleTimeout(@event);
                }
            }
            finally
            {
                await consumer.CloseAsync();
                await session.CloseAsync();
                await connection.CloseAsync();
            }
        }, consumeContext, serviceCancellation.Token);

        var all = new List<Task<PolicyResult>>()
        {
            publisherResult,
            consumeResult
        };

        try
        {
            Task.WaitAny(all.ToArray(), token);
        }catch(OperationCanceledException) { }

        _logger.LogInformation($"Commencing shutdown of {nameof(ActiveMqDelayProcessingService)}");
        
        if (!token.IsCancellationRequested)
        {
            await serviceCancellation.CancelAsync();
            Task.WaitAll(all.Where(x => !x.IsFaulted).ToArray(), _settings.CleanShutdownDelay);
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

    public async Task HandleTimeout(TimeoutPayload payload)
    {
        try
        {
            await _mediator.Send(new EndActiveRoundCommand(payload.SessionId, payload.RoundId));
        }
        catch (EndActiveRoundCommandException x)
        {
            _logger.LogError("Attempt to end Round {RoundId} for Session {SessionId} failed with message {Message}",
                payload.RoundId, payload.SessionId, x.Message);
        }
    }
}