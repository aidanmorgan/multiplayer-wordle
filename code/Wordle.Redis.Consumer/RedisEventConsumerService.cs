using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Polly;
using Polly.Retry;
using StackExchange.Redis;
using Wordle.Clock;
using Wordle.Common;
using Wordle.Events;
using Wordle.Redis.Common;

namespace Wordle.Redis.Consumer;

public class RedisEventConsumerService : IEventConsumerService
{
    private static readonly IDictionary<string, Type> KnownEventTypes = new Dictionary<string, Type>();
    
    public ManualResetEventSlim ReadySignal => throw new NotImplementedException();

    static RedisEventConsumerService()
    {
        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IEvent).IsAssignableFrom(p));
        
        foreach (var type in eventTypes)
        {
            KnownEventTypes[$"wordle.{type.Name}"] = type;
        }
    }
    
    private const int StreamReadCount = 10;

    private readonly RedisConsumerSettings _settings;
    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly ILogger<RedisEventConsumerService> _logger;

    private IDatabase _database;

    public RedisEventConsumerService(RedisConsumerSettings settings, IMediator mediator, IClock clock, ILogger <RedisEventConsumerService> logger)
    {
        _settings = settings;
        _mediator = mediator;
        _clock = clock;
        _logger = logger;
    }
    
    public async Task RunAsync(CancellationToken cts)
    {
        _logger.LogInformation("Listening for events via Redis server: {SettingsRedisHost} key: {SettingsRedisTopic}", _settings.RedisHost, _settings.RedisTopic);

        var result = await RedisRetryPolicy
            .CreateRetryPolicy(_settings.MaxConsumeRetries, (i) => TimeSpan.FromMilliseconds(10))
            .ExecuteAndCaptureAsync(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var result = await _database.StreamReadGroupAsync(
                        _settings.RedisTopic,
                        _settings.InstanceType,
                        _settings.InstanceId,
                        ">",
                        StreamReadCount);

                    foreach (var entry in result)
                    {
                        var typeKey = entry[RedisConstants.EventTypeKey];

                        if (KnownEventTypes.ContainsKey(typeKey))
                        {
                            var payload = entry[RedisConstants.PayloadKey];
                            var obj = (IEvent)JsonConvert.DeserializeObject(payload, KnownEventTypes[typeKey]);

                            if (obj.Timestamp >= _clock.UtcNow().Subtract(_settings.MaximumEventAge))
                            {
                                await _mediator.Publish(obj, cts);
                            }
                            else
                            {
                                _logger.LogInformation("Discarding Event {EventType} with Id {Id} as it is too old", obj.EventType, obj.Id);
                            }
                        }

                        await _database.StreamAcknowledgeAsync(
                            _settings.RedisTopic,
                            _settings.InstanceType, entry.Id);
                    }
                }
                catch(RedisServerException x)
                {
                    _logger.LogWarning(x, "Processing will continue");
                    throw;
                }
                catch(RedisConnectionException x)
                {
                    _logger.LogWarning(x, "Processing will continue");
                    throw;
                }
                catch(TimeoutException x)
                {
                    _logger.LogWarning(x, "Processing will continue");
                    throw;
                }
            }
        });

        if (result.Outcome == OutcomeType.Failure)
        {
            _logger.LogCritical(result.FinalException, $"Exception thrown consuming Events from Redis");
        }
    }
    
    public void Start()
    {
        var muxer = ConnectionMultiplexer.Connect(_settings.RedisHost);
        _database = muxer.GetDatabase();
        
        AsyncContext.Run(async () =>
        {
            if (!(await _database.KeyExistsAsync(_settings.RedisTopic)) ||
                (await _database.StreamGroupInfoAsync(_settings.RedisTopic)).All(x=>x.Name!=_settings.InstanceType))
            {
                await _database.StreamCreateConsumerGroupAsync(_settings.RedisTopic, _settings.InstanceType, "0-0", true);
            }
        });
    }    
}