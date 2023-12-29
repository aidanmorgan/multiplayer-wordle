using System.Net;
using Autofac;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Dictionary;
using Wordle.Model;
using Guess = Wordle.Api.Public.Guess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var container = new AutofacConfigurationBuilder()
    .AddGamePersistence()
    .AddDictionary()
    .AddEventPublishing()
    .Build();

var mediatr = container.Resolve<IMediator>();
var clock = container.Resolve<IClock>();
var dictionary = container.Resolve<IWordleDictionaryService>();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.MapPut("/tenants/{tenantId}/options", async (string tenantId) =>
{
    return HttpStatusCode.NotImplemented;
});

app.MapPut("/tenants/{tenantId}", async (string tenantId) => {
    var activeSession = await mediatr.Send(new GetActiveSessionForTenantQuery("web", tenantId));

    if (activeSession.HasValue)
    {
        app.Logger.Log(LogLevel.Error, $"Cannot submit word, Tenant {tenantId} has active Sessopn {activeSession.Value}");
        return HttpStatusCode.BadRequest;
    }

    var options = (await mediatr.Send(new GetOptionsForTenantQuery("web", tenantId))) ?? new Options();
    
    var newWord = await dictionary.RandomWord(options);
    var newSession = await mediatr.Send(new CreateNewSessionCommand("web", tenantId, newWord, options));

    return HttpStatusCode.Created;
});

app.MapPost("/tenants/{tenantId}/guess", async (string tenantId, [FromBody] Guess guess) =>
{
    var activeSession = await mediatr.Send(new GetActiveSessionForTenantQuery("web", tenantId));

    if (!activeSession.HasValue)
    {
        app.Logger.Log(LogLevel.Error, $"Cannot submit word, no active Session for Tenant {tenantId}");
        return HttpStatusCode.NotFound;
    }

    await mediatr.Send(new AddGuessToRoundCommand(activeSession.Value, guess.UserName, guess.Word, clock.UtcNow()));
    return HttpStatusCode.Accepted;
});

app.MapGet("/tenants/{tenantId}/guesses", ([FromRoute] string tenantId) =>
{

});

app.MapGet("/health", () =>
{

});


app.Run();


namespace Wordle.Api.Public
{
    class Guess
    {
        public string UserName { get; set; }
        public string Word { get; set; }
    }
}