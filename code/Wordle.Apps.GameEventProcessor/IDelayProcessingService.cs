
namespace Wordle.Apps.GameEventProcessor;

public interface IDelayProcessingService
{
    Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken token);

    Task RunAsync(CancellationToken token);
    
    Task HandleTimeout(TimeoutPayload args);
}