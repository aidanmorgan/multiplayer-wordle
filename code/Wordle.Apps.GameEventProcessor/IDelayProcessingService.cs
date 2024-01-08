
using Wordle.Api.Common;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor;

/// <summary>
/// Used to enqueue work that is to be done at a later date to the underlying implementation
/// </summary>
public interface IDelayProcessingService 
{
    ManualResetEventSlim ReadySignal { get; }
    
    Task ScheduleRoundUpdate(VersionId session, VersionId round, DateTimeOffset executionTime, CancellationToken token);

    Task RunAsync(CancellationToken token);
    
    Task HandleTimeout(TimeoutPayload args);
}