using System.Reactive.Concurrency;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Wordle.Apps.Common;
using Wordle.Aws.Common;

namespace Wordle.Api.Realtime;

public class Program
{
    public static void Main(string[] args)
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.FullName, "d66c2093-964e-4f94-9a24-49e7b6cabfd2");

        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(x =>
        {
            var conf = new AutofacConfigurationBuilder(x);
            conf.AddDictionary();
            conf.AddKafkaEventConsuming(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);
            conf.AddGamePersistence();

            conf.Callback(x =>
            {
                x.RegisterType<EventConsumerBackgroundService>().As<IHostedService>().AsImplementedInterfaces().SingleInstance();
                x.RegisterType<WordleTenantService>().As<IWordleTenantService>().AsImplementedInterfaces().SingleInstance();
            });
            
            conf.InitialiseDefaults();
        }));

        builder.Services.AddControllers();
        
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)
        });
        
        app.MapControllers();

        app.Run();
    }
}

public class EventConsumerBackgroundService : IHostedService
{
    private readonly IEventConsumerService _consumerService;

    public EventConsumerBackgroundService(IEventConsumerService svx)
    {
        _consumerService = svx;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _consumerService.RunAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // do nothing
        return Task.CompletedTask;
    }
}