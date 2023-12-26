﻿using MediatR;
using Queries;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Dictionary;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Persistence;
using Wordle.Persistence.Dynamo;

namespace Wordle.CommandHandlers;

public struct Container
{
    public Tenant Tenant { get; set; }
    public Options Options { get; set; }
}

public class CreateNewSessionCommandHandler : IRequestHandler<CreateNewSessionCommand, Guid>
{
    private readonly IClock _clock;
    private readonly IGameUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IWordleDictionaryService _dictionaryService;
    private readonly ILogger _logger;


    public CreateNewSessionCommandHandler(ILogger logger, IClock clock, IGameUnitOfWork unitOfWork, IMediator mediator, IWordleDictionaryService dictSvc)
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

        var tenantId = Tenant.CreateTenantId(request.TenantType, request.TenantName);
        var options = await _mediator.Send(new GetOptionsForTenantQuery(request.TenantType, request.TenantName), cancellationToken);

        if (options == null)
        {
            // if there are no default options registered for this tenant then we need to create one
            options = new Options()
            {
                Id = Ulid.NewUlid().ToGuid(),
                TenantId = tenantId,
                CreatedAt = _clock.UtcNow()
            };
            
            await _unitOfWork.Options.AddAsync(tenantId, options);
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
        await _unitOfWork.Sessions.AddAsync(tenantId, session);

        await _unitOfWork.Rounds.AddAsync(new Round()
        {
            Id = roundId,
            SessionId = sessionId,
            CreatedAt = _clock.UtcNow(),
            State = RoundState.ACTIVE
        });


        // we have the options we're meant to use for the session now, either a new set, or ones loaded from a tenant's
        // configuration, so add them to the store. MAKING EXTRA SURE that the sessionid is set and the tenantid is not set
        // otherwise we'll corrupt everything
        var sessionOptions = options.Clone();
        sessionOptions.Id = Ulid.NewUlid().ToGuid();
        sessionOptions.CreatedAt = _clock.UtcNow();

        await _unitOfWork.Options.AddAsync(session, sessionOptions);
        
        await _unitOfWork.SaveAsync();
        
        _logger.Log($"Created new Session: {sessionId} with initial Round: {roundId}");
        
        await _mediator.Publish(new NewSessionStarted(sessionId, request.TenantName), cancellationToken);
        await _mediator.Publish(new NewRoundStarted(sessionId, roundId, session.ActiveRoundEnd.Value), cancellationToken);
        
        return sessionId;
    }
}