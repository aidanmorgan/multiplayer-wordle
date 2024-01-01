// See https://aka.ms/new-console-template for more information

using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using GrapeCity.Documents.Text;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Queries;
using Wordle.Apps.Common;
using Wordle.Aws.Common;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class Program : INotificationHandler<RoundEnded>
{
    
    private readonly ILogger _logger;
    private readonly IRenderer _renderer;
    private readonly IMediator _mediator;
    private readonly IAmazonS3 _s3;


    private readonly IAmazonSQS _sqs;
    private readonly IEventConsumerService _eventConsumerService;

    public static void Main()
    {
        EnvironmentVariablesExtensions.SetDefault("INSTANCE_TYPE", "Board-Generator") ;
        EnvironmentVariablesExtensions.SetDefault("INSTANCE_ID", "9a81b0d6-e62b-4e11-b7a7-7040095de6f8") ;
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddImagePersistence()
            .AddKafkaEventPublishing(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddKafkaEventConsuming(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddRenderer()
            .RegisterSelf(typeof(Program));

        var container = configBuilder.Build();

        var program = container.Resolve<Program>(); 
        program.Run();
    }
    
    public Program(IRenderer renderer, IAmazonS3 se, IEventConsumerService svc, ILogger logger)
    {
        _renderer = renderer;
        _s3 = _s3;
        _eventConsumerService = svc;
        _logger = logger;
    }

    private void Run()
    {
        var token = new CancellationTokenSource();
        Task.WaitAll(_eventConsumerService.RunAsync(token.Token));
    }

    public async Task Handle(RoundEnded ev, CancellationToken token)
    {
        var session = await _mediator.Send(new GetSessionByIdQuery(ev.SessionId), token);
        if (session == null)
        {
            _logger.Log($"Attempting to generate for Session {ev.SessionId}, but could not load.");
            return;
        }
                            
        var round = session?.Rounds.FirstOrDefault(x => x.Id == ev.RoundId);
        if (round == null)
        {
            _logger.Log($"Attempting to generate for Session {ev.SessionId} and Round {ev.RoundId} but Round could not be found.");
            return;
        }

        using var stream = new MemoryStream();
        _renderer.Render(session?.Rounds.Select(x => new DisplayWord(x.Guess, x.Result)).ToList() ?? new List<DisplayWord>(), null, stream);

        var filename = $"boards/{ev.SessionId}.{ev.RoundId}.png";
                            
        await _s3.UploadObjectFromStreamAsync(EnvironmentVariables.BoardBucketName, filename, stream, new Dictionary<string, object>(), token);
                            
        _logger.Log($"Uploaded board image: {filename}");
        
    }
}