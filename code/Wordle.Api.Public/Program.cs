using System.Net;
using System.Threading.RateLimiting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Dictionary;
using Wordle.Model;

namespace Wordle.Api.Public;

public class Program
{
    public const string EventSourceType = "PublicApi";
    public static void Main(string[] args)
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.FullName, "76846c80-17ab-480c-ba65-0cec7086552a");

        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(x =>
        {
            var conf = new AutofacConfigurationBuilder(x);
            conf.AddDictionary();
            conf.AddRedisEventPublisher(EventSourceType, EnvironmentVariables.InstanceId);
            conf.AddGamePersistence();
            
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