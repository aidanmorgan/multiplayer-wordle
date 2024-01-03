using System.Reactive.Concurrency;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Wordle.Api.Common;
using Wordle.Apps.Common;

namespace Wordle.Api.Realtime;

public class Program
{
    public static void Main(string[] args)
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.GetName().Name, "d66c2093-964e-4f94-9a24-49e7b6cabfd2");

        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(x =>
        {
            var conf = new AutofacConfigurationBuilder(x);
            conf.AddPostgresDictionary();
            conf.AddActiveMqEventConsumer(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId, true);
            conf.AddPostgresPersistence();

            conf.Callback(x =>
            {
                x.RegisterType<WebsocketTenantService>()
                    .As<IWebsocketTenantService>()
                    .SingleInstance()
                    .AsImplementedInterfaces()
                    .SingleInstance();
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