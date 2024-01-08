using System.Threading.RateLimiting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Wordle.Api.Public.Handlers;
using Wordle.Apps.Common;

namespace Wordle.Api.Public;

public class Program
{
    public const string EventSourceType = "PublicApi";
    public static void Main(string[] args)
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.GetName().Name, "76846c80-17ab-480c-ba65-0cec7086552a");

        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(x =>
        {
            var conf = new AutofacConfigurationBuilder(x);
            conf.AddActiveMqEventPublisher(EventSourceType, EnvironmentVariables.InstanceId, true);
            conf.AddPostgresPersistence();
            conf.AddPostgresDictionary();

            conf.Callback(x =>
            {
                x.RegisterType<LocalDiskImageLoader>()
                    .As<IBoardImageLoader>()
                    .WithParameter(new PositionalParameter(0, EnvironmentVariables.ImagesDirectory))
                    .SingleInstance();
            });
            
            conf.InitialiseDefaults();
        }));
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        builder.Services.AddRateLimiter((x) =>
        {
            x.AddTokenBucketLimiter("newgame-limit", y =>
            {
                y.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
                y.AutoReplenishment = true;
                y.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
                y.QueueLimit = 5;
                y.TokenLimit = 5;
                y.TokensPerPeriod = 2;
            });

            x.AddTokenBucketLimiter("guess-limit", y =>
            {
                y.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
                y.AutoReplenishment = true;
                y.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
                y.QueueLimit = 5;
                y.TokenLimit = 5;
                y.TokensPerPeriod = 2;
            });
        });
        
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

        app.MapControllers();       
        

        app.Run();
    }
}

public class Guess
{
    public string Username { get; set; }
    public string Word { get; set; }
}