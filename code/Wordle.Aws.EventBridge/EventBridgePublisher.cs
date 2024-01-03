using System.Net;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Wordle.Clock;
using Wordle.Common;
using Wordle.Events;

namespace Wordle.Aws.EventBridge;

// an implementation of INotificationHandlers that push ALL event types out to an event bridge instance
// with the detail set appropriately for integration with other downstream lambdas
public class EventBridgePublisher : IAllEventHandlers
{
    public const string EventDetailPrefix = "wordle.";
    
    private static readonly Guid InstanceId = Ulid.NewUlid().ToGuid();
    private readonly IAmazonEventBridge _client;
    private readonly string _busName;
    private readonly ILogger<EventBridgePublisher> _logger;
    private readonly IClock _clock;
    private readonly string _instanceId;
    private readonly string _sourceType;

    public EventBridgePublisher(IAmazonEventBridge client, string busName, string sourceType, string instanceId, ILogger<EventBridgePublisher> logger, IClock clock)
    {
        _client = client;
        _busName = busName;
        _sourceType = sourceType;
        _instanceId = instanceId;
        _logger = logger;
    }

    public async Task Handle(RoundEnded notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    public async Task Handle(GuessAdded notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    public async Task Handle(NewRoundStarted notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    public async Task Handle(NewSessionStarted notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    public async Task Handle(SessionEndedWithFailure notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    public async Task Handle(SessionEndedWithSuccess notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }
    
    public async Task Handle(SessionTerminated notification, CancellationToken cancellationToken)
    {
        await PublishEvent(notification, cancellationToken);
    }

    private async Task PublishEvent<T>(T notification, CancellationToken cancellationToken) where T : IEvent
    {
        if (notification.EventSourceType == _sourceType && notification.EventSourceId == _instanceId)
        {
            _logger.LogInformation("Ignoring event {EventSourceType}#{Id} as it is from {SourceType}#{InstanceId}.", notification.EventSourceType, notification.Id, _sourceType, _instanceId);
            return;
        }
        
        notification.EventSourceId = _instanceId;
        notification.EventSourceType = _sourceType;
        notification.Timestamp = _clock.UtcNow();
        
        var payload = JsonConvert.SerializeObject(notification);
        
        var entries = new List<PutEventsRequestEntry>() 
        {
            new()
            {
                Detail = payload,
                Source = $"{EventDetailPrefix}{InstanceId}",
                DetailType =$"{EventDetailPrefix}{notification.GetType().Name}",
                EventBusName = _busName
            }
        };
        
        var response = await _client.PutEventsAsync(new PutEventsRequest()
        {
            Entries = entries
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Publishing to EventBridge returned a {ResponseHttpStatusCode} status code", response.HttpStatusCode);
            response.Entries.ForEach(x => _logger.LogError("Error: {ErrorCode} : {ErrorMessage}", x.ErrorCode, x.ErrorMessage));
        }
        else
        {
            entries.ForEach(x => _logger.LogInformation("Published: {Detail}", x.Detail));
        }
    }

}