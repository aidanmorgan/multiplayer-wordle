using MediatR;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor;

// used to enqueue any active sessions into the delayed processing queue in the case that we've had a complete
// broker failure and we need to recover 
public interface IInitialisationService
{
    public Task<bool> RunAsync(CancellationToken token);
}

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