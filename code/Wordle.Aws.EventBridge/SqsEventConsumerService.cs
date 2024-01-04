using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Wordle.Common;
using Wordle.Events;

namespace Wordle.Aws.EventBridge;

public class SqsEventConsumerOptions
{
    public int MaxNumberOfMessages { get; init; } = 10;
    public int WaitTimeSeconds { get; init; } = 20;
    public int VisibilityTimeout { get; init; } = 3;
}

public class SqsEventConsumerService : IEventConsumerService
{
    private static readonly Dictionary<string,Type> KnownEvents;

    private readonly string _sqsUrl;
    private readonly IAmazonSQS _sqs;
    private readonly SqsEventConsumerOptions _options;
    private readonly IMediator _mediator;
    private readonly string _instanceId;
    private readonly string _sourceType;
    private readonly ILogger<SqsEventConsumerService> _logger;

    static SqsEventConsumerService()
    {
        KnownEvents = new Dictionary<string, Type>();
        
        var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IEvent).IsAssignableFrom(p));
        
        foreach (var type in eventTypes)
        {
            KnownEvents[$"wordle.{type.Name}"] = type;
        }
    }

    public static SqsEventConsumerService Create(string url, string sourceType, string instanceId, IContainer c, SqsEventConsumerOptions? opts = null)
    {
        return new SqsEventConsumerService(url,
            sourceType,
            instanceId,
            c.Resolve<IAmazonSQS>(),
            c.Resolve<IMediator>(),
            c.Resolve<ILogger<SqsEventConsumerService>>(), 
            opts ?? new SqsEventConsumerOptions());
    }
    
    public ManualResetEventSlim ReadySignal => throw new NotImplementedException();

    public SqsEventConsumerService(string sqsUrl, string sourceType, string instanceId, IAmazonSQS sqs, IMediator mediatr, ILogger<SqsEventConsumerService> logger,
        SqsEventConsumerOptions? options = null)
    {
        _sqs = sqs;
        _sourceType = sourceType;
        _instanceId = instanceId;
        _sqsUrl = sqsUrl;
        _mediator = mediatr;
        _logger = logger;

        _options = options ?? new SqsEventConsumerOptions();
    }

    public async Task RunAsync(CancellationToken token)
    {
        _logger.LogInformation("Listening for game events on: {Url}", _sqsUrl);
        
        while (!token.IsCancellationRequested)
        {
            var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _sqsUrl,
                MaxNumberOfMessages = _options.MaxNumberOfMessages,
                WaitTimeSeconds = _options.WaitTimeSeconds,
                VisibilityTimeout = _options.VisibilityTimeout
            }, token);

            foreach (var message in messages?.Messages ?? [])
            {
                var decoded = JsonConvert.DeserializeObject<JObject>(message.Body);
                if (decoded != null)
                {
                    // this janky code is just here to get the event type out of the message
                    // so we can determine how to cast the event as appropriate when we want to
                    // process.
                    var detailType = decoded.GetValue("detail-type")?.Value<string>();

                    if (!string.IsNullOrEmpty(detailType) && KnownEvents.TryGetValue(detailType, out var eventType))
                    {
                        var extractedEvent = (IEvent)decoded["detail"].ToObject(eventType);

                        if (extractedEvent.EventSourceType == _sourceType && extractedEvent.EventSourceId == _instanceId)
                        {
                            _logger.LogInformation("Ignoring event  {EventType}#{EventId} as it is from {SourceType}#{SourceId}", extractedEvent.EventType, extractedEvent.Id, _sourceType, _instanceId);
                        }
                        else
                        {
                            await _mediator.Publish(extractedEvent, token);
                        }
                    }
                }

                // delete the message so it doesnt get reprocessed
                await _sqs.DeleteMessageAsync(new DeleteMessageRequest()
                {
                    QueueUrl = _sqsUrl,
                    ReceiptHandle = message.ReceiptHandle
                });
            }

            if (messages.Messages.Count == 0)
            {
                _logger.LogInformation("No events received in last {TimeSeconds} seconds", _options.WaitTimeSeconds);
            }
        }
    }
}
