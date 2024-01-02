using Autofac.Extensions.DependencyInjection;
using Wordle.Apps.Common;

namespace Wordle.Api.Private;

public class Program
{
    public static void Main(string[] args)
    {
        EnvironmentVariables.SetDefaultInstanceConfig(typeof(Program).Assembly.FullName, "5eca0ee2-a147-4272-8ea6-f4b8a4e6dd76");

        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(x =>
        {
            var conf = new AutofacConfigurationBuilder(x);
            conf.AddDynamoDictionary();
            conf.AddKafkaEventPublishing(EnvironmentVariables.InstanceType, EnvironmentVariables.InstanceId);
            conf.AddDynamoPersistence();
            
            conf.InitialiseDefaults();
        }));

        // Add services to the container.
        builder.Services.AddAuthorization();

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

        app.Run();
    }
}