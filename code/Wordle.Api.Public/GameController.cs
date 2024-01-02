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
        var existingSession = await _mediator.Send(new GetActiveSessionForTenantQuery("web", tenantId));
        if (existingSession.HasValue)
        {
            _logger.LogError("Could not find Session for Tenant web${TenantId}", tenantId);
            return new BadRequestResult();
        }

        var options = (await _mediator.Send(new GetOptionsForTenantQuery("web", tenantId))) ?? new Options();

        try
        {
            var newSession =
                await _mediator.Send(new CreateNewSessionCommand(
                    "web",
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
        var session = await _mediator.Send(new GetActiveSessionForTenantQuery("web", tenantId));
        if (!session.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        try
        {
            await _mediator.Send(new AddGuessToRoundCommand(session.Value, g.Username, g.Word, _clock.UtcNow()));
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
        var sessionId = await _mediator.Send(new GetActiveSessionForTenantQuery("web", tenantId));
        if (!sessionId.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        var session = (await _mediator.Send(new GetSessionByIdQuery(sessionId.Value)
        {
            IncludeOptions = false,
            IncludeWord = false
        }));
        
        return new JsonResult(session);
    }

    [HttpGet("/v1/tenants/{tenantId}/options")]
    [ProducesResponseType<Options>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetOptions(string tenantId)
    {
        var sessionId = await _mediator.Send(new GetActiveSessionForTenantQuery("web", tenantId));
        if (!sessionId.HasValue)
        {
            _logger.LogError("Could not find Session with Tenant {TenantId}", tenantId);
            return new NotFoundResult();
        }

        var session = (await _mediator.Send(new GetSessionByIdQuery(sessionId.Value)
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
        var options = await _mediator.Send(new GetOptionsForTenantQuery("web", tenantId)) ?? new Options();
        
        

        return new OkResult();
    }
    
}
