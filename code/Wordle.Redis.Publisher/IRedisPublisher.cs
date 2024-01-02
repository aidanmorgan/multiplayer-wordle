using Autofac;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Redis.Common;

namespace Wordle.Redis.Publisher;

public interface IRedisPublisher : IStartable
{
    Task Send(IEvent ev, CancellationToken token);
}

public class DefaultRedisPublisher : IRedisPublisher
{
    private readonly RedisSettings _settings;
    private readonly IClock _clock;
    private readonly ILogger<DefaultRedisPublisher> _logger;

    private IDatabase _database;
    private List<NameValueEntry> _baseKeys;

    public DefaultRedisPublisher(RedisSettings settings, IClock clock, ILogger<DefaultRedisPublisher> logger)
    {
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }
    
    public void Start()
    {
        var muxer = ConnectionMultiplexer.Connect(_settings.RedisHost);
        _database = muxer.GetDatabase();

        _baseKeys = new List<NameValueEntry>()
        {
            new NameValueEntry(RedisConstants.EventSourceTypeKey, _settings.InstanceType),
            new NameValueEntry(RedisConstants.EventSourceIdKey, _settings.InstanceId)
        };
    }

    public async Task Send(IEvent ev, CancellationToken token)
    {
        // this check makes sure that the event we're generating hasn't already been dispatched somewhere
        // else as these values are only set by the publisher
        if (!string.IsNullOrEmpty(ev.EventSourceId) || !string.IsNullOrEmpty(ev.EventSourceType))
        {
            return;
        }

        ev.EventSourceType = _settings.InstanceType;
        ev.EventSourceId = _settings.InstanceId;
        ev.Timestamp = _clock.UtcNow();
        
        var nameValueEntries = new List<NameValueEntry>(_baseKeys)
        {
            new NameValueEntry(RedisConstants.EventTypeKey, $"wordle.{ev.GetType().Name}"),
            new NameValueEntry(RedisConstants.EventIdKey, ev.Id.ToString()),
            new NameValueEntry(RedisConstants.PayloadKey, JsonConvert.SerializeObject(ev))
        };

        var result = await _database.StreamAddAsync(
            _settings.RedisTopic, 
            nameValueEntries.ToArray(),
            "*");
        
        _logger.LogInformation("Publishing {SerializeObject} from {EvEventSourceType}#{EvEventSourceId}#{Result}", JsonConvert.SerializeObject(ev), ev.EventSourceType, ev.EventSourceId, result);
    }
}