using System.Net;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Newtonsoft.Json;
using Wordle.Events;
using Wordle.Logger;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Wordle.Aws.EventBridgeImpl;

// an implementation of INotificationHandlers that push ALL event types out to an event bridge instance
// with the detail set appropriately for integration with other downstream lambdas
public class EventBridgePublisher : IEventBridgeEventPublisher
{
    private static readonly Guid InstanceId = Ulid.NewUlid().ToGuid();
    private readonly IAmazonEventBridge _client;
    private readonly string _busName;
    private readonly ILogger _logger;

    public EventBridgePublisher(IAmazonEventBridge client, string busName, ILogger logger)
    {
        _client = client;
        _busName = busName;
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
        var payload = JsonConvert.SerializeObject(notification);
        
        var entries = new List<PutEventsRequestEntry>() 
        {
            new()
            {
                Detail = payload,
                Source = $"wordle.{InstanceId}",
                DetailType =$"wordle.{notification.GetType().Name}",
                EventBusName = _busName
            }
        };
        
        var response = await _client.PutEventsAsync(new PutEventsRequest()
        {
            Entries = entries
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.Log($"Publishing to EventBridge returned a {response.HttpStatusCode} status code.");
            response.Entries.ForEach(x => _logger.Log($"Error: {x.ErrorCode} : {x.ErrorMessage}"));
        }
        else
        {
            entries.ForEach(x => _logger.Log($"Published: {x.Detail}"));
        }
    }

}