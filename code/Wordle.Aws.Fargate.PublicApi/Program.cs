using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries;
using Wordle.Aws.Common;
using Wordle.Clock;
using Wordle.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var container = new AutofacConfigurationBuilder().AddGamePersistence().AddEventBridge().Build();
var mediatr = container.Resolve<IMediator>();
var clock = container.Resolve<IClock>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}


app.MapPost("/tenants/{tenantId}/submit", async (string tenantId, [FromHeader] string userName, [FromBody] string word) =>
{
    var activeSession = await mediatr.Send(new GetActiveSessionForTenantQuery("web", tenantId));

    if (!activeSession.HasValue)
    {
        return HttpStatusCode.NotFound;
    }

    await mediatr.Send(new AddGuessToRoundCommand(activeSession.Value, userName, word, clock.UtcNow()));
    return HttpStatusCode.Accepted;
});

app.MapGet("/tenants/{tenantId}/guesses", ([FromRoute] string tenantId) =>
{

});

app.MapGet("/health", () =>
{

});


app.Run();
