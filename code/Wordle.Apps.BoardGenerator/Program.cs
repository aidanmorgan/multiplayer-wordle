// See https://aka.ms/new-console-template for more information

using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using GrapeCity.Documents.Text;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wordle.Apps.Common;
using Wordle.Aws.Common;
using Wordle.Clock;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Queries;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class Program 
{
    
    private readonly ILogger _logger;
    private readonly IRenderer _renderer;
    private readonly IMediator _mediator;
    private readonly IAmazonS3 _s3;


    private readonly IAmazonSQS _sqs;
    private readonly IEventConsumerService _eventConsumerService;

    public static void Main()
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.FullName, "9a81b0d6-e62b-4e11-b7a7-7040095de6f8");
        
        var configBuilder = new AutofacConfigurationBuilder()
            .AddGamePersistence()
            .AddImagePersistence()
            .AddKafkaEventPublishing(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddKafkaEventConsuming(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId)
            .AddRenderer()
            .RegisterSelf(typeof(Program));

        configBuilder.Callback(x =>
        {
            x.RegisterType<BoardGeneratorHandlers>().AsImplementedInterfaces();
        });

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
        Task.WaitAny(_eventConsumerService.RunAsync(token.Token));
        
        _logger.Log("Exiting....");
    }
}