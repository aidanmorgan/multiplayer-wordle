using Autofac;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
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
    private readonly RedisPublisherSettings _settings;
    private readonly IClock _clock;
    private readonly ILogger<DefaultRedisPublisher> _logger;
    private readonly AsyncRetryPolicy _publishRetryPolicy;

    private IDatabase _database;
    private List<NameValueEntry> _baseKeys;
    private Task _cleanerTask;

    public DefaultRedisPublisher(RedisPublisherSettings settings, IClock clock, ILogger<DefaultRedisPublisher> logger)
    {
        _settings = settings;
        _clock = clock;
        _logger = logger;
        
        _publishRetryPolicy = 
            RedisRetryPolicy.CreateRetryPolicy(_settings.MaxPublishRetries, (i) => TimeSpan.FromMilliseconds(5)); 
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

        _cleanerTask = Task.Run(async () =>
        {
            while (true)
            {
                var stop = _clock.UtcNow().Subtract(_settings.SeenEventExpiry);
                var count = await _database.SortedSetRemoveRangeByScoreAsync(
                    _settings.SeenEventIdKey,
                    _settings.SeenEventWindowStart.ToUnixTimeMilliseconds(), // the time to use for the start of the check
                    stop.ToUnixTimeMilliseconds(),  // the time to use for the end of the check
                    Exclude.Stop);

                if (count > 0)
                {
                    _logger.LogInformation("Removed {Count} Event id's from tracking in {Key}", count, _settings.SeenEventIdKey);
                }
                
                await Task.Delay(_settings.SeenEventInterval);
            }
        });
    }

    public async Task Send(IEvent ev, CancellationToken token)
    {
        // this check makes sure that the event we're generating hasn't already been dispatched somewhere
        // else as these values are only set by the publisher
        if (!string.IsNullOrEmpty(ev.EventSourceId) || !string.IsNullOrEmpty(ev.EventSourceType))
        {
            return;
        }

        // this is a check to see that we haven't already seen the event before, what we do is store the event id
        // in a sorted set with the current timestamp as the score. Later on in the process we will automatically
        // remove elements from the sorted set that are older that a certain timeframe.
        //
        // The score is the current unix time milliseconds that will then allow the reaper process lower down to
        // occur
        if (!await _database.SortedSetAddAsync(
                _settings.SeenEventIdKey, 
                $"{ev.Id}",
                _clock.UtcNow().ToUnixTimeMilliseconds(),       // TODO : work out if the event time makes more sense here
                When.NotExists))
        {
            _logger.LogInformation("Skipping submitting potential duplicate Event {EventType} with Id {Id}", ev.EventType, ev.Id);
            return;
        }

        ev.EventSourceType = _settings.InstanceType;
            ev.EventSourceId = _settings.InstanceId;
            ev.Timestamp = _clock.UtcNow();

            var serializedEvent = JsonConvert.SerializeObject(ev);
        
            var nameValueEntries = new List<NameValueEntry>(_baseKeys)
            {
                new NameValueEntry(RedisConstants.EventTypeKey, $"wordle.{ev.GetType().Name}"),
                new NameValueEntry(RedisConstants.EventIdKey, ev.Id.ToString()),
                new NameValueEntry(RedisConstants.PayloadKey, serializedEvent)
            };

            var publishResult = await _publishRetryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                var result = await _database.StreamAddAsync(
                    _settings.RedisTopic,
                    nameValueEntries.ToArray(),
                    "*");

                _logger.LogInformation("Publishing {SerializeObject} from {EvEventSourceType}#{EvEventSourceId}#{Result}",
                    serializedEvent, ev.EventSourceType, ev.EventSourceId, result);
            });

            if (publishResult.Outcome == OutcomeType.Failure)
            {
                _logger.LogCritical(publishResult.FinalException, "Exception publishing {Event}", serializedEvent);
            }
        
    }
}