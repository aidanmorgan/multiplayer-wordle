using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using Wordle.Aws.Common;
using Wordle.Aws.Kafka.Common;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Logger;

namespace Wordle.Aws.Kafka.Publisher;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly KafkaSettings _settings;

    public KafkaEventPublisher(KafkaSettings settings, IClock clock, ILogger logger)
    {
        _settings = settings;
        
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = $"{settings.InstanceType}#{settings.InstanceId}",
            AllowAutoCreateTopics = true
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();

        _clock = clock;
        _logger = logger;
    }
    
    
    public async Task Handle(GuessAdded notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(NewRoundStarted notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(NewSessionStarted notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(RoundEnded notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(SessionEndedWithFailure notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(SessionEndedWithSuccess notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    public async Task Handle(SessionTerminated notification, CancellationToken cancellationToken)
    {
        await SendNotification(notification);
    }

    private async Task SendNotification(IEvent ev)
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
        });

        _logger.Log($"Publishing {JsonConvert.SerializeObject(ev)} from {ev.EventSourceType}#{ev.EventSourceId}");
    }
    
    public void Dispose()
    {
        _producer?.Dispose();
    }
}