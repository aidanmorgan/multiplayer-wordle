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
using Wordle.Aws.EventBridgeImpl;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class Program
{
    private static readonly Guid InstanceId = Guid.NewGuid();

    private const int MAX_EVENT_MESSAGES = 10;
    private const int MAX_TIMEOUT_MESSAGES = 5;
    private const int READ_WAIT_TIME_SECONDS = 10;
    private const int VISIBILITY_TIMEOUT_SECONDS = 5;

    
    private readonly ILogger _logger;
    private readonly IRenderer _renderer;
    private readonly IMediator _mediator;
    private readonly IAmazonS3 _s3;


    private readonly IAmazonSQS _sqs;

    public static void Main()
    {
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddImagePersistence()
            .AddEventPublishing()
            .AddEventHandling()
            .AddRenderer();
        
        var program = new Program(configBuilder.Build()); 
        program.Run();
    }
    
    public Program(IContainer container)
    {
        _sqs = container.Resolve<IAmazonSQS>();
        _renderer = container.Resolve<IRenderer>();
        _mediator = container.Resolve<IMediator>();
        _s3 = container.Resolve<IAmazonS3>();

        _logger = container.Resolve<ILogger>();
    }

    private void Run()
    {
        var token = new CancellationTokenSource();
        
        var boardGeneratorTask = Task.Run(async () => { await BoardProcessingLoop(token); }, token.Token);
        
        try
        {
            Task.WaitAll(new Task[] { boardGeneratorTask }, token.Token);
        }
        catch (AggregateException x)
        {
            foreach(var innerException in x.Flatten().InnerExceptions)
            {
                _logger.Log(innerException.Message);
            }
        }
    }

    private readonly string RoundEndEventName = $"{EventBridgePublisher.EventDetailPrefix}{nameof(RoundEnded)}";

    private async Task BoardProcessingLoop(CancellationTokenSource token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var messages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = EnvironmentVariables.BoardGeneratorQueueUrl,
                    MaxNumberOfMessages = MAX_EVENT_MESSAGES,
                    WaitTimeSeconds = READ_WAIT_TIME_SECONDS,
                    VisibilityTimeout = VISIBILITY_TIMEOUT_SECONDS
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

                        if (!string.IsNullOrEmpty(eventType) && eventType.Equals(RoundEndEventName))
                        {
                            var ev = decoded["detail"].ToObject<RoundEnded>();

                            var session = await _mediator.Send(new GetSessionByIdQuery(ev.SessionId));
                            if (!session.HasValue)
                            {
                                _logger.Log($"Attempting to generate for Session {ev.SessionId}, but could not load.");
                                continue;
                            }
                            
                            var round = session?.Rounds.FirstOrDefault(x => x.Id == ev.RoundId);
                            if (round == null)
                            {
                                _logger.Log($"Attempting to generate for Session {ev.SessionId} and Round {ev.RoundId} but Round could not be found.");
                            }

                            using var stream = new MemoryStream();
                            _renderer.Render(session?.Rounds.Select(x => new DisplayWord(x.Guess, x.Result)).ToList() ?? new List<DisplayWord>(), null, stream);

                            var filename = $"boards/{ev.SessionId}.{ev.RoundId}.png";
                            
                            await _s3.UploadObjectFromStreamAsync(EnvironmentVariables.BoardBucketName, filename, stream, new Dictionary<string, object>(), token.Token);
                            
                            _logger.Log($"Uploaded board image: {filename}");
                        }

                        await _sqs.DeleteMessageAsync(new DeleteMessageRequest()
                        {
                            QueueUrl = EnvironmentVariables.BoardGeneratorQueueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        });
                    }
                }
            }
            catch (Exception x)
            {
                _logger.Log("exception", $"Exception thrown processing timeouts.");
                _logger.Log(x.Message);
            }
        }
    }
}