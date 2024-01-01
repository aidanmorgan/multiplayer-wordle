// See https://aka.ms/new-console-template for more information

using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Queries;
using Wordle.Apps.Common;
using Wordle.Aws.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using IContainer = Autofac.IContainer;

namespace Wordle.Apps.GameEventProcessor;

public class Program : 
    INotificationHandler<NewSessionStarted>, 
    INotificationHandler<RoundEnded>, 
    INotificationHandler<NewRoundStarted>,
    INotificationHandler<RoundExtended>
{
    private static readonly Guid InstanceId = Guid.NewGuid();
    
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    
    private readonly IAmazonSQS _sqs;
    private readonly IEventConsumerService _eventHandler;

    public static void Main()
    {
        EnvironmentVariablesExtensions.SetDefault("INSTANCE_TYPE", "Game-Event-Loop") ;
        EnvironmentVariablesExtensions.SetDefault("INSTANCE_ID", "d66c2093-964e-4f94-9a24-49e7b6cabfd2") ;
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddDictionary()
            .AddKafkaEventPublishing(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddKafkaEventConsuming(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);

        configBuilder.RegisterSelf(typeof(Program));
        var container = configBuilder.Build();
        
        var program = container.Resolve<Program>(); 
        program.Run();
    }
    
    public Program(IMediator mediator, IClock clock, IAmazonSQS sqs, ILogger logger, IEventConsumerService svc)
    {
        _mediator = mediator;
        _clock = clock;
        _sqs = sqs;
        _logger = logger;
        _eventHandler = svc;
    }

    private void Run()
    {
        var token = new CancellationTokenSource();
        Task.WaitAll(_eventHandler.RunAsync(token.Token));
        _logger.Log("Exiting.");
    }

    public Task Handle(NewSessionStarted x, CancellationToken cancellationToken)
    {
        _logger.Log("New session started!");
        return Task.CompletedTask;
    }

    public async Task Handle(RoundEnded detail, CancellationToken cancellationToken)
    {
        var q = await _mediator.Send(new GetSessionByIdQuery(detail.SessionId));
        if (q == null)
        {
            _logger.Log($"Could not load Session with id {detail.SessionId}.");
            return;
        }

        var session = q.Session;

        if (session.State != SessionState.ACTIVE)
        {
            _logger.Log($"Could not update Session with id {detail.SessionId}, it is not ACTIVE.");
            return;
        }

        var rounds = q.Rounds;
        var options = q.Options;

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
    public async Task Handle(NewRoundStarted detail, CancellationToken cancellationToken)
    {
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
    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
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

}