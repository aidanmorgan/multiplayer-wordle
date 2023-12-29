// See https://aka.ms/new-console-template for more information

using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using MediatR;
using Newtonsoft.Json;
using Queries;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;

namespace Wordle.Apps.TimeoutProcessor;

public class Program
{
    private static readonly Guid InstanceId = Guid.NewGuid();
    
    // the maximum number of messages to read from the queue at once
    private const int MAX_TIMEOUT_MESSAGES = 5;
    
    // the amount of time to wait for messages before retrying
    private const int READ_WAIT_TIME_SECONDS = 10;
    
    // how long each message batch should be allowed for processing before making the message
    // visible to other readers
    private const int VISIBILITY_TIMEOUT_SECONDS = 5;
    
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    private readonly IGameUnitOfWorkFactory _uowFactory;
    
    private readonly IAmazonSQS _sqs;
    

    public static void Main()
    {
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddDictionary()
            .AddEventPublishing()
            .AddEventHandling();
        
        var program = new Program(configBuilder.Build()); 
        program.Run();
    }
    
    public Program(IContainer container)
    {
        _mediator = container.Resolve<IMediator>();
        _clock = container.Resolve<IClock>();
        _sqs = container.Resolve<IAmazonSQS>();

        _logger = container.Resolve<ILogger>();
        _uowFactory = container.Resolve<IGameUnitOfWorkFactory>();
    }

    private void Run()
    {
        var token = new CancellationTokenSource();
        
        var gameRenewalQueue = Task.Run(async () => { await TimeoutProcessingLoopAsync(token); }, token.Token);
        
        try
        {
            Task.WaitAll(new Task[] { gameRenewalQueue }, token.Token);
        }
        catch (AggregateException x)
        {
            foreach(var innerException in x.Flatten().InnerExceptions)
            {
                _logger.Log(innerException.Message);
            }
        }
    }

    private async Task TimeoutProcessingLoopAsync(CancellationTokenSource token)
    {
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
                }, token.Token);

                foreach (var message in messages?.Messages ?? [])
                {
                    var payload = JsonConvert.DeserializeObject<TimeoutPayload>(message.Body);

                    if (payload != null)
                    {
                        await HandleTimeout(payload, token);
                    }
                        
                    await _sqs.DeleteMessageAsync(new DeleteMessageRequest()
                    {
                        QueueUrl = EnvironmentVariables.TimeoutQueueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    });
                }
            }
            catch (Exception x)
            {
                _logger.Log("exception", $"Exception thrown processing timeouts.");
                _logger.Log(x.Message);
            }
        }
    }

    // Called when the event bus publishes a NewRoundStarted event, will enqueue an automated timeout
    // that will extend the round if required
    public async Task SubmitTimeout(NewRoundStarted detail, CancellationToken cancellationToken)
    {
        if (detail.SessionId == Guid.Empty || detail.RoundId == Guid.Empty)
        {
            _logger.Log($"Ignoring invalid {nameof(NewRoundStarted)} message.");
            return; 
        }
        
        _logger.Log($"Received {nameof(NewRoundStarted)} event for Round {detail.RoundId}. Starting timer.");

        try
        {
            await _sqs.SendMessageAsync(new SendMessageRequest(EnvironmentVariables.TimeoutQueueUrl,
                JsonConvert.SerializeObject(TimeoutPayload.Create(detail)))
            {
                DelaySeconds = (int)Math.Max(Math.Ceiling(detail.RoundExpiry.Subtract(_clock.UtcNow()).TotalSeconds), 0)
            }, cancellationToken);
        }
        catch (Exception x)
        {
            throw;
        }
    }

    // Called when the event bus publishes a RoundExtended event, will enqueue an automated timeout
    // that will extend the round if required.
    public async Task SubmitTimeout(RoundExtended notification, CancellationToken cancellationToken)
    {
        if (notification.SessionId == Guid.Empty || notification.RoundId == Guid.Empty)
        {
            _logger.Log($"Ignoring invalid {nameof(RoundExtended)} message.");
            return; 
        }

        _logger.Log($"Received {nameof(RoundExtended)} event for Round {notification.RoundId}. Starting timer.");

        try
        {
            await _sqs.SendMessageAsync(new SendMessageRequest(EnvironmentVariables.TimeoutQueueUrl,
                JsonConvert.SerializeObject(TimeoutPayload.Create(notification)))
            {
                DelaySeconds =
                    (int)Math.Max(Math.Ceiling(notification.RoundExpiry.Subtract(_clock.UtcNow()).TotalSeconds), 0)
            }, cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }
    
    private async Task HandleTimeout(TimeoutPayload p, CancellationTokenSource cancellationToken)
    {
        if (p.SessionId == Guid.Empty || p.RoundId == Guid.Empty)
        {
            _logger.Log($"Ignoring invalid {nameof(TimeoutPayload)} message.");
            return; 
        }
        
        var qr = await _mediator.Send(new GetSessionByIdQuery(p.SessionId));

        if (!qr.HasValue)
        {
            _logger.Log($"Cannot end active round for session {p.SessionId}, no Session found.");
            return;
        }

        var session = qr.Value.Session;

        if (session.State != SessionState.ACTIVE)
        {
            _logger.Log($"Cannot end active round for Session {session.Id}, it is not ACTIVE.");
            return;
        }

        if (!session.ActiveRoundId.HasValue)
        {
            _logger.Log($"Cannot end active round for Session {session.Id}, there is no active round.");
            return;
        }

        if (!session.ActiveRoundEnd.HasValue)
        {
            _logger.Log($"Cannot end active round for Session {session.Id}. there is no active round end set.");
            return;
        }

        if (!p.RoundId.Equals(session.ActiveRoundId.Value) )
        {
            _logger.Log($"Cannot end Round {p.RoundId} for Session {session.Id}. it is not the active round.");
            return;
        }

        try
        {
            await _mediator.Send(new EndActiveRoundCommand(session.Id));
        }
        catch (CommandException)
        {
            // ignore
        }
    }
}