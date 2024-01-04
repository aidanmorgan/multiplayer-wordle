using System.Text;
using Confluent.Kafka;
using MediatR;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Microsoft.Extensions.Logging;
using Wordle.Common;
using Wordle.Events;

namespace Wordle.Kafka.Consumer;

public class KafkaEventConsumerService : IEventConsumerService
{
    private static readonly IDictionary<string, Type> KnownEventTypes;

    public ManualResetEventSlim ReadySignal => throw new NotImplementedException();


    static KafkaEventConsumerService()
    {
        KnownEventTypes = new Dictionary<string, Type>();
        
        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IEvent).IsAssignableFrom(p));
        
        foreach (var type in eventTypes)
        {
            KnownEventTypes[$"wordle.{type.Name}"] = type;
        }
    }
    
    private static string? GetEventTypeFromHeader(ConsumeResult<Ignore, string> m)
    {
        var headerPair = m.Message.Headers.FirstOrDefault(x => x.Key == "event-type");
        if (headerPair == null)
        {
            return null;
        }

        var headerValue = Encoding.UTF8.GetString(headerPair.GetValueBytes());
        return headerValue;
    }
    

    private readonly IMediator _mediator;
    private readonly ILogger<KafkaEventConsumerService> _logger;
    private readonly KafkaEventConsumerOptions _options;

    private const int RetryCount = 10;

    private static readonly AsyncRetryPolicy RetryPolicy = Policy.
        Handle<Exception>()
        .WaitAndRetryAsync(RetryCount, x => TimeSpan.FromMilliseconds(100), 
        onRetry: (x, i, c) =>
        {
            // clear the consumer logic up, we'll attempt to reconnect on the retry
            c.CloseConsumer();
        });

    public KafkaEventConsumerService(KafkaEventConsumerOptions options, IMediator mediator, ILogger<KafkaEventConsumerService> logger)
    {
        _options = options;
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task RunAsync(CancellationToken token)
    {
        _logger.LogInformation("Receiving events from Topic: {SettingsTopic} for bootstrap servers: {SettingsBootstrapServers}", _options.Topic, _options.BootstrapServers);

        var ctx = new Context();
        
        while (!token.IsCancellationRequested)
        {
            var result = await RetryPolicy.ExecuteAndCaptureAsync(async (ctx) =>
            {
                var consumer = ctx.GetConsumer(() =>
                {
                    var consumerConfig = new ConsumerConfig()
                    {
                        BootstrapServers = _options.BootstrapServers,

                        // we use the following to control visibility to ensure that only one consumer is processing
                        // data from the queue at a time
                        GroupId = _options.InstanceType,
                        GroupInstanceId = _options.InstanceId,
                        EnableAutoCommit = true
                    };
                    var builder = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                    builder.Subscribe(_options.Topic);
                    return builder;
                });
                
                var message = consumer!.Consume(token);
                var typeName = GetEventTypeFromHeader(message);
                if (string.IsNullOrEmpty(typeName))
                {
                    return;
                }

                if (!KnownEventTypes.ContainsKey(typeName))
                {
                    return;
                }

                var decoded = (IEvent?)JsonConvert.DeserializeObject(message.Message.Value, KnownEventTypes[typeName]);
                if (decoded == null || decoded.EventSourceType == _options.InstanceType)
                {
                    _logger.LogInformation("Ignoring Event {DecodedEventType}#{DecodedId} as it is from {EventSourceType}#{EventSourceId}", decoded.EventType, decoded.Id, decoded.EventSourceType, decoded.EventSourceId);
                    return;
                }

                await _mediator.Publish(decoded, token);
            }, ctx);

            if (result.Outcome == OutcomeType.Failure)
            {
                _logger.LogCritical(result.FinalException, "Aborting processing. {InstanceType}#{InstanceId} had too many failures after {RetryCount} attempts", _options.InstanceType, _options.InstanceId, RetryCount);
            }
        }
    }
}

public static class ContextExtensions
{
    private static readonly string Key = "client";
    
    public static void CloseConsumer(this Context context)
    {
        if (context.ContainsKey(Key))
        {
            var value = context[Key] as IConsumer<Ignore, string>;

            try
            {
                value?.Close();
            }
            catch (Exception) { }
            
            context.Remove(Key);
        }    
    }

    public static IConsumer<Ignore, string> GetConsumer(this Context context, Func<IConsumer<Ignore, string>> factory)
    {
        if (context.TryGetValue(Key, out var value))
        {
            return (value as IConsumer<Ignore, string>)!;
        }

        var result = factory();
        context[Key] = result;
        return result;
    }
}