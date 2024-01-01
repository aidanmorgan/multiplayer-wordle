using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Newtonsoft.Json;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Logger;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class SqsDelayProcessingService : AbstractDelayProcessingService
{
    // the maximum number of messages to read from the queue at once
    private const int MAX_TIMEOUT_MESSAGES = 5;
    
    // the amount of time to wait for messages before retrying
    private const int READ_WAIT_TIME_SECONDS = 20;
    
    // how long each message batch should be allowed for processing before making the message
    // visible to other readers
    private const int VISIBILITY_TIMEOUT_SECONDS = 5;

    
    private readonly IClock _clock;
    private readonly IAmazonSQS _sqs;

    public SqsDelayProcessingService(IMediator mediator, IClock clock, IAmazonSQS sqs, ILogger logger) : base(mediator, logger)
    {
        _sqs = sqs;
        _clock = clock;
    }

    public override async Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken cancellationToken)
    {
        try
        {
            await _sqs.SendMessageAsync(new SendMessageRequest(EnvironmentVariables.TimeoutQueueUrl,
                JsonConvert.SerializeObject(new TimeoutPayload(sessionId, roundId)))
            {
                DelaySeconds = (int)Math.Max(Math.Ceiling(executionTime.Subtract(_clock.UtcNow()).TotalSeconds), 0)
            }, cancellationToken);
        }
        catch (Exception x)
        {
            throw;
        }
    }

    public override async Task RunAsync(CancellationToken token)
    {
        Logger.Log($"Listening for timeout payloads on {EnvironmentVariables.TimeoutQueueUrl}");
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
                    Logger.Log($"No messages received in last {READ_WAIT_TIME_SECONDS} seconds.");
                }
            }
            catch (Exception x)
            {
                Logger.Log("exception", $"Exception thrown processing timeouts.");
                Logger.Log(x.Message);
            }
        }
    }
}