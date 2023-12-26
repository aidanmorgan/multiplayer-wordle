// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using MediatR;
using Queries;
using Wordle.Aws.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using IContainer = Autofac.IContainer;

namespace Aws.Fargate.EventHandler;

public class Program
{
    private const int MAX_EVENT_MESSAGES = 32;
    private const int MAX_TIMEOUT_MESSAGES = 32;
    
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    
    private readonly IAmazonSQS _sqs;

    public static void Main()
    {
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddDictionary()
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
    }

    private void Run()
    {
        var token = new CancellationTokenSource();
        
        var eventProcessingQueue = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = EnvironmentVariables.EventQueueUrl,
                    MaxNumberOfMessages = MAX_EVENT_MESSAGES
                }, token.Token);

                foreach (var message in messages?.Messages ?? [])
                {
                    
                }
            }
        }, token.Token);

        var gameRenewalQueue = Task.Run(async () =>
        {
            var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = EnvironmentVariables.TimeoutQueueUrl,
                MaxNumberOfMessages = MAX_TIMEOUT_MESSAGES
            }, token.Token);

            foreach (var message in messages?.Messages ?? [])
            {
                using var stream = new MemoryStream( Encoding.UTF8.GetBytes( message.Body ) );
                var payload = await JsonSerializer.DeserializeAsync<Payload>(stream, cancellationToken: token.Token);

                if (payload != null)
                {
                    await Handle(payload, token);
                }
                else
                {
                    _logger.Log($"Received a Round Extension Payload that cannot be deserialized.");
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


    private async Task HandleRoundEndEvent(RoundEnded detail)
    {
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
    public async Task Handle(NewRoundStarted notification, CancellationToken cancellationToken)
    {
        _logger.Log($"Received {nameof(NewRoundStarted)} event for Round {notification.RoundId}. Sending to SQS.");
        
        await _sqs.SendMessageAsync(new SendMessageRequest(EnvironmentVariables.TimeoutQueueUrl, JsonSerializer.Serialize(Payload.Create(notification)))
        {
            DelaySeconds = (int)Math.Max(Math.Ceiling(notification.RoundExpiry.Subtract(_clock.UtcNow()).TotalSeconds), 0)
        }, cancellationToken);
    }

    // Called when the event bus publishes a RoundExtended event, will enqueue an automated timeout
    // that will extend the round if required.
    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
        _logger.Log($"Received {nameof(RoundExtended)} event for Round {notification.RoundId}. Sending to SQS.");
        
        await _sqs.SendMessageAsync(new SendMessageRequest(EnvironmentVariables.TimeoutQueueUrl,JsonSerializer.Serialize(Payload.Create(notification)))
        {
            DelaySeconds = (int)Math.Max(Math.Ceiling(notification.RoundExpiry.Subtract(_clock.UtcNow()).TotalSeconds), 0)
        }, cancellationToken);    
    }
    
    private async Task Handle(Payload p, CancellationTokenSource cancellationToken)
    {
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
        
        // try to end the round, if it can't be ended then it wil be automatically extended anyway
        await _mediator.Send(new EndActiveRoundCommand(session.Id));
    }

    
    
}