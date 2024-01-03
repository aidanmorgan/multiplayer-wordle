using MediatR;
using Wordle.Common;
using Wordle.Events;

namespace Wordle.Apps.Common;

public class EventPublisherServiceDecoratorImpl : IAllEventHandlers
{
    private readonly IEventPublisherService _publisher;

    public EventPublisherServiceDecoratorImpl(IEventPublisherService publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(GuessAdded notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(NewRoundStarted notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(NewSessionStarted notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(RoundEnded notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(SessionEndedWithFailure notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(SessionEndedWithSuccess notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(SessionTerminated notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }

    public async Task Handle(BaseEvent notification, CancellationToken cancellationToken)
    {
        await _publisher.Publish(notification, cancellationToken);
    }
}