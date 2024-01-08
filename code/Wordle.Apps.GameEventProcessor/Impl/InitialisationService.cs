using MediatR;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class InitialisationService : IInitialisationService
{
    private readonly IMediator _mediator;
    private readonly IDelayProcessingService _delayProcessingService;

    public InitialisationService(IMediator mediator, IDelayProcessingService svc)
    {
        _mediator = mediator;
        _delayProcessingService = svc;
    }
    
    public async Task<bool> RunAsync(CancellationToken token)
    {
        var activeSessions = await _mediator.Send(new GetAllActiveSessionsQuery(), token);

        foreach (var pair in activeSessions)
        {
            await _delayProcessingService.ScheduleRoundUpdate(pair.Session, pair.Round, pair.RoundExpiry.Value, token);
        }

        return !token.IsCancellationRequested;
    }
}