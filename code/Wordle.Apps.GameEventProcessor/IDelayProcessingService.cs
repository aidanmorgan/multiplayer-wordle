
using Wordle.Api.Common;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor;

public interface IDelayProcessingService 
{
    ManualResetEventSlim ReadySignal { get; }
    
    Task ScheduleRoundUpdate(VersionId session, VersionId round, DateTimeOffset executionTime, CancellationToken token);

    Task RunAsync(CancellationToken token);
    
    Task HandleTimeout(TimeoutPayload args);
}