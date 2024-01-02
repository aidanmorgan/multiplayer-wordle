using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Microsoft.Extensions.Logging;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Kafka.Common;

namespace Wordle.Kafka.Publisher;

public interface IKafkaPublisher
{
    Task Send(IEvent ev, CancellationToken token);
    void Start();
}

public class KafkaPublisher : IKafkaPublisher
{
    private readonly KafkaSettings _settings;
    private readonly IClock _clock;
    private readonly ILogger<KafkaPublisher> _logger;

    private IProducer<Null,string> _producer;

    public KafkaPublisher(KafkaSettings settings, IClock clock, ILogger<KafkaPublisher> logger)
    {
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }

    public void Start()
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _settings.BootstrapServers }).Build();

        AsyncContext.Run(async () =>
        {
            try
            {
                var metadata =  adminClient.GetMetadata(_settings.Topic, TimeSpan.FromSeconds(5));

                if (metadata.Topics.Any(x => x.Topic == _settings.Topic))
                {
                    return;
                }
                
                await adminClient.CreateTopicsAsync(new TopicSpecification[]
                {
                    new TopicSpecification { Name = _settings.Topic, ReplicationFactor = 1, NumPartitions = 1 }
                });
            }
            catch (CreateTopicsException e)
            {
                _logger.LogError(e, "An error occured creating topic {Topic}: {ErrorReason}", e.Results[0].Topic, e.Results[0].Error.Reason);
            }
        });
        
        _logger.LogInformation("Publishing events to Topic: {SettingsTopic} for bootstrap servers: {SettingsBootstrapServers}", _settings.Topic, _settings.BootstrapServers);
        
        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = $"{_settings.InstanceType}#{_settings.InstanceId}",
            AllowAutoCreateTopics = true
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    
    public async Task Send(IEvent ev, CancellationToken token)
    {
        // this check makes sure that the event we're generating hasn't already been dispatched somewhere
        // else as these values are only set by the publisher
        if (!string.IsNullOrEmpty(ev.EventSourceId) || !string.IsNullOrEmpty(ev.EventSourceType))
        {
            return;
        }
        
        var headers = new Headers
        {
            new Header("event-type",  Encoding.UTF8.GetBytes($"wordle.{ev.GetType().Name}")),
            new Header("event-tenant", Encoding.UTF8.GetBytes(ev.Tenant)),
            new Header("event-instance-type", Encoding.UTF8.GetBytes(_settings.InstanceType)),
            new Header("event-instance-id", Encoding.UTF8.GetBytes(_settings.InstanceId))
        };

        ev.EventSourceType = _settings.InstanceType;
        ev.EventSourceId = _settings.InstanceId;
        ev.Timestamp = _clock.UtcNow();

        await _producer.ProduceAsync(_settings.Topic, new Message<Null, string>()
        {
            Headers = headers,
            Value = JsonConvert.SerializeObject(ev)
        }, token);

        _logger.LogInformation("Publishing {SerializeObject} from {EvEventSourceType}#{EvEventSourceId}", JsonConvert.SerializeObject(ev), ev.EventSourceType, ev.EventSourceId);
    }

}