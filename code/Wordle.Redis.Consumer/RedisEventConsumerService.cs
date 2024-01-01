using Autofac;
using MediatR;
using Newtonsoft.Json;
using Nito.AsyncEx;
using StackExchange.Redis;
using Wordle.Aws.Common;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Redis.Common;

namespace Wordle.Redis.Consumer;

public class RedisEventConsumerService : IEventConsumerService
{
    private static readonly IDictionary<string, Type> KnownEventTypes = new Dictionary<string, Type>();
    public const int StreamReadCount = 10;

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

    
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    private IDatabase _database;
    private readonly RedisSettings _settings;

    public RedisEventConsumerService(RedisSettings settings, IMediator mediator, ILogger logger)
    {
        _settings = settings;
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task RunAsync(CancellationToken cts)
    {
        _logger.Log($"Listening for events via Redis server: {_settings.RedisHost} key: {_settings.RedisTopic}.");
        
        while (!cts.IsCancellationRequested)
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

                    await _mediator.Publish(obj, cts);
                }
                
                await _database.StreamAcknowledgeAsync(
                    _settings.RedisTopic, 
                    _settings.InstanceType, entry.Id);
            }

//            await Task.Delay(TimeSpan.FromMilliseconds(5), cts);
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