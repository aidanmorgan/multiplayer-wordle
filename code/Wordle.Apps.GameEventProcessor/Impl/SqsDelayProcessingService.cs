using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class SqsDelayProcessingService : IDelayProcessingService
{
    // the maximum number of messages to read from the queue at once
    private const int MAX_TIMEOUT_MESSAGES = 5;
    
    // the amount of time to wait for messages before retrying
    private const int READ_WAIT_TIME_SECONDS = 20;
    
    // how long each message batch should be allowed for processing before making the message
    // visible to other readers
    private const int VISIBILITY_TIMEOUT_SECONDS = 5;

    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<SqsDelayProcessingService> _logger;

    public SqsDelayProcessingService(IMediator mediator, IClock clock, IAmazonSQS sqs, ILogger<SqsDelayProcessingService> logger)
    {
        _mediator = mediator;
        _sqs = sqs;
        _clock = clock;
        _logger = logger;
    }

    public async Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken cancellationToken)
    {
        try
        {
            await _sqs.SendMessageAsync(new SendMessageRequest(EnvironmentVariables.TimeoutQueueUrl,
                JsonConvert.SerializeObject(new TimeoutPayload(sessionId, roundId, executionTime)))
            {
                DelaySeconds = (int)Math.Max(Math.Ceiling(executionTime.Subtract(_clock.UtcNow()).TotalSeconds), 0)
            }, cancellationToken);
        }
        catch (Exception x)
        {
            throw;
        }
    }

    public async Task RunAsync(CancellationToken token)
    {
        _logger.LogInformation("Listening for timeout payloads on {Url}", EnvironmentVariables.TimeoutQueueUrl);
        while (!token.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = EnvironmentVariables.TimeoutQueueUrl,
                    MaxNumberOfMessages = MAX_TIMEOUT_MESSAGES,
                    WaitTimeSeconds = READ_WAIT_TIME_SECONDS,
                    VisibilityTimeout = VISIBILITY_TIMEOUT_SECONDS
                }, token);

                foreach (var message in messages?.Messages ?? [])
                {
                    var payload = JsonConvert.DeserializeObject<TimeoutPayload>(message.Body);

                    if (payload != null)
                    {
                        await HandleTimeout(payload);
                    }
                        
                    await _sqs.DeleteMessageAsync(new DeleteMessageRequest()
                    {
                        QueueUrl = EnvironmentVariables.TimeoutQueueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    }, token);
                }

                if (messages.Messages.Count == 0)
                {
                    _logger.LogInformation("No messages received in last {WaitTimeSeconds} seconds", READ_WAIT_TIME_SECONDS);
                }
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Exception thrown processing timeouts");
            }
        }
    }
    
    public async Task HandleTimeout(TimeoutPayload payload)
    {
        try
        {
            await _mediator.Send(new EndActiveRoundCommand(payload.SessionId, payload.RoundId));
        }
        catch (CommandException x)
        {
            _logger.LogError(x, "Attempt to end Round {RoundId} for Session {SessionId} failed", payload.RoundId, payload.SessionId);
        }
    }
}