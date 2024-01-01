// See https://aka.ms/new-console-template for more information

using System.Reflection;
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
    // the maximum number of messages to read from the queue at once
    private const int MAX_TIMEOUT_MESSAGES = 5;
    
    // the amount of time to wait for messages before retrying
    private const int READ_WAIT_TIME_SECONDS = 20;
    
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
        EnvironmentVariablesExtensions.SetDefault("INSTANCE_TYPE", "Timeout-Processor") ;
        EnvironmentVariablesExtensions.SetDefault("INSTANCE_ID", "85c9a94e-8e0c-4984-90bc-4c0403bd44b7") ;

        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddDictionary()
            .AddKafkaEventPublishing(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .RegisterSelf(typeof(Program));

        var container = configBuilder.Build();
        var program = container.Resolve<Program>(); 
        program.Run();
    }
    
    public Program(IMediator mediator, IClock clock, IAmazonSQS sqs, IGameUnitOfWorkFactory factory, ILogger logger)
    {
        _mediator = mediator;
        _clock = clock;
        _sqs = sqs;

        _uowFactory = factory;
        _logger = logger;
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
        _logger.Log($"Listening for timeout payloads on {EnvironmentVariables.TimeoutQueueUrl}");
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

                if (messages.Messages.Count == 0)
                {
                    _logger.Log($"No messages received in last {READ_WAIT_TIME_SECONDS} seconds.");
                }
            }
            catch (Exception x)
            {
                _logger.Log("exception", $"Exception thrown processing timeouts.");
                _logger.Log(x.Message);
            }
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

        if (qr == null)
        {
            _logger.Log($"Cannot end active round for session {p.SessionId}, no Session found.");
            return;
        }

        var session = qr.Session;

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