using StackExchange.Redis;
using Wordle.Common;
using Wordle.Events;

namespace Wordle.Redis.Publisher;

public class RedisEventPublisher : IAllEventHandlers
{
    private readonly List<NameValueEntry> _baseKeys;
    private readonly IRedisPublisher _publisher;

    public RedisEventPublisher(IRedisPublisher publisher)
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