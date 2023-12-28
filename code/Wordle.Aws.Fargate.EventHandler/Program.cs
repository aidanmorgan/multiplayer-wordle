// See https://aka.ms/new-console-template for more information

using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Queries;
using Wordle.Aws.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using IContainer = Autofac.IContainer;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Aws.Fargate.EventHandler;

public class Program
{
    private static readonly Guid InstanceId = Guid.NewGuid();
    
    private const int MAX_EVENT_MESSAGES = 5;
    private const int MAX_TIMEOUT_MESSAGES = 5;
    private const int READ_WAIT_TIME_SECONDS = 10;
    
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
        
        var eventProcessingQueue = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = EnvironmentVariables.EventQueueUrl,
                        MaxNumberOfMessages = MAX_EVENT_MESSAGES,
                        WaitTimeSeconds = READ_WAIT_TIME_SECONDS
                    }, token.Token);

                    foreach (var message in messages?.Messages ?? [])
                    {
                        var decoded = JsonConvert.DeserializeObject<JObject>(message.Body);
                        if (decoded != null)
                        {
                            // this janky code is just here to get the event type out of the message
                            // so we can determine how to cast the event as appropriate when we want to
                            // process.
                            var eventType = decoded.GetValue("detail-type").Value<string>();

                            if (!string.IsNullOrEmpty(eventType))
                            {
                                if (eventType.EndsWith(nameof(NewSessionStarted)))
                                {
                                    await Handle(decoded["detail"].ToObject<NewSessionStarted>(), token.Token);
                                }
                                else if (eventType.EndsWith(nameof(RoundEnded)))
                                {
                                    await Handle(decoded["detail"].ToObject<RoundEnded>(), token.Token);
                                }
                                else if (eventType.EndsWith(nameof(NewRoundStarted)))
                                {
                                    await SubmitTimeout(decoded["detail"].ToObject<NewRoundStarted>(), token.Token);
                                }
                                else if (eventType.EndsWith(nameof(RoundExtended)))
                                {
                                    await SubmitTimeout(decoded["detail"].ToObject<RoundExtended>(), token.Token);
                                }
                                else
                                {
                                    _logger.Log($"Ignoring event {eventType}");
                                }
                            }
                        }
                        
                        // delete the message so it doesnt get reprocessed
                        await _sqs.DeleteMessageAsync(new DeleteMessageRequest()
                        {
                            QueueUrl = EnvironmentVariables.EventQueueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        });
                    }
                }
                catch (Exception x)
                {
                    _logger.Log("exception", $"Exception thrown processing events.");
                    _logger.Log(x.Message);
                }
            }
        }, token.Token);

        var gameRenewalQueue = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = EnvironmentVariables.TimeoutQueueUrl,
                        MaxNumberOfMessages = MAX_TIMEOUT_MESSAGES,
                        WaitTimeSeconds = READ_WAIT_TIME_SECONDS
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
        }, token.Token);

        try
        {
            Task.WaitAll(new Task[] { eventProcessingQueue, gameRenewalQueue }, token.Token);
        }
        catch (AggregateException x)
        {
            foreach(var innerException in x.Flatten().InnerExceptions)
            {
                _logger.Log(innerException.Message);
            }
        }
    }

    private Task Handle(NewSessionStarted x, CancellationToken cancellationToken)
    {
        if (x.SessionId == Guid.Empty)
        {
            _logger.Log($"Ignoring invalid {nameof(NewSessionStarted)} message.");
            return Task.CompletedTask; 
        }
        
        return Task.CompletedTask;
    }

    private async Task Handle(RoundEnded detail, CancellationToken cancellationToken)
    {
        if (detail.SessionId == Guid.Empty || detail.RoundId == Guid.Empty)
        {
            _logger.Log($"Ignoring invalid {nameof(RoundExtended)} message.");
            return; 
        }
        
        var q = await _mediator.Send(new GetSessionByIdQuery(detail.SessionId));

        if (!q.HasValue)
        {
            _logger.Log($"Could not load Session with id {detail.SessionId}.");
            return;
        }

        var session = q.Value.Session;

        if (session.State != SessionState.ACTIVE)
        {
            _logger.Log($"Could not update Session with id {detail.SessionId}, it is not ACTIVE.");
            return;
        }

        var rounds = q.Value.Rounds;
        var options = q.Value.Options;

        var lastRound = rounds.FirstOrDefault(x => x.Id == detail.RoundId);
        if (lastRound == null)
        {
            _logger.Log(
                $"Could not find Round with id {detail.RoundId} in the rounds for Session {detail.SessionId}");
            return;
        }

        if (string.Equals(lastRound.Guess, session.Word, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.Log($"Correct answer found, ending Session {detail.SessionId} with SUCCESS");
            await _mediator.Send(new EndSessionCommand(detail.SessionId, session.Word, true, false));
            return;
        }
        else
        {
            if (rounds.Count < options.NumberOfRounds)
            {
                var roundId = await _mediator.Send(new CreateNewRoundCommand(detail.SessionId));
                _logger.Log($"Created new round {roundId} for Session {detail.SessionId}");
                return;
            }
            else
            {
                _logger.Log($"Incorrect final guess, ending Session {detail.SessionId}.");
                await _mediator.Send(new EndSessionCommand(detail.SessionId, session.Word, false, true));
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
        
        _logger.Log($"Received {nameof(NewRoundStarted)} event for Round {detail.RoundId}. Sending to SQS.");

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

        _logger.Log($"Received {nameof(RoundExtended)} event for Round {notification.RoundId}. Sending to SQS.");

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