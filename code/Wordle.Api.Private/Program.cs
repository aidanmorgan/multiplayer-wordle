using Autofac;
using MediatR;
using Wordle.Apps.Common;
using Wordle.Clock;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var container = new AutofacConfigurationBuilder().AddGamePersistence().AddEventPublishing().Build();
var mediatr = container.Resolve<IMediator>();
var clock = container.Resolve<IClock>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



app.Run();
