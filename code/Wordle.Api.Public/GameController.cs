using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Dictionary;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Api.Public;

public class GameController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly IWordleDictionaryService _dictionary;
    private readonly ILogger<GameController> _logger;

    public GameController(IMediator mediator,  IClock clock, IWordleDictionaryService dictionary, ILogger<GameController> logger)
    {
        _mediator = mediator;
        _clock = clock;
        _dictionary = dictionary;
        _logger = logger;
    }
    
    [EnableRateLimiting("newgame-limit")]
    [HttpPost("/v1/tenants/{tenantId}")]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<IActionResult> NewGame(string tenantId)
    {
        var existingSession = await _mediator.Send(new GetActiveSessionForTenantQuery(tenantId));
        if (existingSession.HasValue)
        {
            _logger.LogError("Could not find Session for Tenant {TenantId}", tenantId);
            return new BadRequestResult();
        }

        var options = (await _mediator.Send(new GetOptionsForTenantQuery(tenantId))) ?? new Options();

        try
        {
            var newSession =
                await _mediator.Send(new CreateNewSessionCommand(
                    tenantId,
                    await _dictionary.RandomWord(options),
                    options));

            return new CreatedResult();
        }
        catch (CommandException x)
        {
            _logger.LogError(x, "Could not create new Session for Tenant web#{TenantId}", tenantId);
            return new BadRequestResult();
        }
    }

    [EnableRateLimiting("guess-limit")]
    [HttpPost("/v1/tenants/{tenantId}/guesses")]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<IActionResult> SubmitGuess(string tenantId, [FromBody] Guess g)
    {
        var session = await _mediator.Send(new GetActiveSessionForTenantQuery(tenantId));
        if (!session.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        try
        {
            // don't have to use versions here, guesses are single-value and never updated
            await _mediator.Send(new AddGuessToRoundCommand(session.Value.Id, session.Value.Version, g.Username, g.Word, _clock.UtcNow()));
            return new CreatedResult();
        }
        catch (CommandException x)
        {
            _logger.LogError(x, "Could not add Guess to Session {Session} for Tenant web#{TenantId}", session, tenantId);
            return new BadRequestResult();
        }
    }

    [HttpGet("/v1/tenants/{tenantId}")]
    [ProducesResponseType<SessionQueryResult>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetSession(string tenantId)
    {
        var sessionId = await _mediator.Send(new GetActiveSessionForTenantQuery(tenantId));
        if (!sessionId.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        var session = (await _mediator.Send(new GetSessionByIdQuery(sessionId.Value.Id, sessionId.Value.Version)
        {
            IncludeOptions = false,
            IncludeWord = false
        }));
        
        return new JsonResult(session);
    }
    
    [HttpGet("/v1/tenants/{tenantId}/board")]
    [ProducesResponseType<Options>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [Produces("image/png")]
    public async Task<IActionResult> GetBoard(string tenantId,
        [FromServices]IBoardImageHandler imageHandler)
    {
        var sessionId = await _mediator.Send(new GetActiveSessionForTenantQuery(tenantId));
        if (!sessionId.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        var res = (await _mediator.Send(new GetSessionByIdQuery(sessionId.Value.Id, sessionId.Value.Version)
        {
            IncludeRounds = false,
            IncludeOptions = true,
            IncludeWord = false
        }));

        // either there's an active round, or we have the round that is the last one played for the game
        var roundId = res.Session.ActiveRoundId ?? res.Rounds.Last()?.Id;
        return await imageHandler.GetImageForBoard(res.Session.Id, roundId, res.Rounds.Select(x => (Guid?)x.Id).ToList().IndexOf(roundId));
    }
    

    [HttpGet("/v1/tenants/{tenantId}/options")]
    [ProducesResponseType<Options>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetOptions(string tenantId)
    {
        var sessionId = await _mediator.Send(new GetActiveSessionForTenantQuery(tenantId));
        if (!sessionId.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        var session = (await _mediator.Send(new GetSessionByIdQuery(sessionId.Value.Id, sessionId.Value.Version)
        {
            IncludeRounds = false,
            IncludeOptions = true,
            IncludeWord = false
        }));
        
        return new JsonResult(session.Options);        
    }
    
    [HttpPut("/v1/tenants/{tenantId}/options")]
    [ProducesResponseType<Options>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [Produces("application/json")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> UpdateOptions(string tenantId, IFormCollection values)
    {
        var options = await _mediator.Send(new GetOptionsForTenantQuery(tenantId)) ?? new Options();
        
        

        return new OkResult();
    }
    
}
