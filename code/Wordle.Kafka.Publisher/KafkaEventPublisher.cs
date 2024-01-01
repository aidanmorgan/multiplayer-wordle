using System.Text;
using Autofac;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Wordle.Aws.Common;
using Wordle.Kafka.Common;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Logger;

namespace Wordle.Kafka.Publisher;

public class KafkaEventPublisher : IEventPublisher
{
    private readonly IKafkaPublisher _publisher;


    public KafkaEventPublisher(IKafkaPublisher publisher)
    {
        _publisher = publisher;
    }
    
    
    public async Task Handle(GuessAdded notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(NewRoundStarted notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(NewSessionStarted notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(RoundEnded notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(SessionEndedWithFailure notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(SessionEndedWithSuccess notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }

    public async Task Handle(SessionTerminated notification, CancellationToken cancellationToken)
    {
        await _publisher.Send(notification, cancellationToken);
    }
}