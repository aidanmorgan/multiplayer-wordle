using MediatR;
using Microsoft.Extensions.Logging;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Dictionary;
using Wordle.Events;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Queries;

namespace Wordle.CommandHandlers;

public class CreateNewSessionCommandHandler : IRequestHandler<CreateNewSessionCommand, Guid>
{
    private readonly IClock _clock;
    private readonly IGameUnitOfWorkFactory _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IWordleDictionaryService _dictionaryService;
    private readonly ILogger<CreateNewSessionCommandHandler> _logger;


    public CreateNewSessionCommandHandler(ILogger<CreateNewSessionCommandHandler> logger, IClock clock, IGameUnitOfWorkFactory unitOfWork, IMediator mediator, IWordleDictionaryService dictSvc)
    {
        _logger = logger;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _dictionaryService = dictSvc;
    }

    public async Task<Guid> Handle(CreateNewSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionId = Ulid.NewUlid().ToGuid();
        var roundId = Ulid.NewUlid().ToGuid();

        var options = await _mediator.Send(new GetOptionsForTenantQuery(request.TenantName), cancellationToken);

        var uow = _unitOfWork.Create();
        
        if (options == null)
        {
            // if there are no default options registered for this tenant then we need to create one
            options = new Options()
            {
                Id = Ulid.NewUlid().ToGuid(),
                TenantId = request.TenantName,
                CreatedAt = _clock.UtcNow()
            };
            
            await uow.Options.AddAsync(request.TenantName, options);
        }
        
        var word = request.Word;
        if (string.IsNullOrEmpty(word))
        {
            word = await _dictionaryService.RandomWord(options);
        }

        var session = new Session()
        {
            Id = sessionId,
            CreatedAt = _clock.UtcNow(),
            Word = word,
            State = SessionState.ACTIVE,
            ActiveRoundId = roundId,
            ActiveRoundEnd = _clock.UtcNow().AddSeconds(options.InitialRoundLength),
        };
        await uow.Sessions.AddAsync(request.TenantName, session);

        var round = new Round()
        {
            Id = roundId,
            SessionId = sessionId,
            CreatedAt = _clock.UtcNow(),
            State = RoundState.ACTIVE
        };
        
        await uow.Rounds.AddAsync(round);


        // we have the options we're meant to use for the session now, either a new set, or ones loaded from a tenant's
        // configuration, so add them to the store. MAKING EXTRA SURE that the sessionid is set and the tenantid is not set
        // otherwise we'll corrupt everything
        var sessionOptions = options.Clone();
        sessionOptions.Id = Ulid.NewUlid().ToGuid();
        sessionOptions.CreatedAt = _clock.UtcNow();

        await uow.Options.AddAsync(session, sessionOptions);
        await uow.SaveAsync();
        
        _logger.LogInformation("Created new Session: {SessionId} with initial Round: {RoundId}", sessionId, roundId);
        
        await _mediator.Publish(new NewSessionStarted(session.Tenant, sessionId, session.Version), cancellationToken);
        await _mediator.Publish(new NewRoundStarted(session.Tenant, sessionId, session.Version, roundId, round.Version, session.ActiveRoundEnd.Value, true), cancellationToken);
        
        return sessionId;
    }
}