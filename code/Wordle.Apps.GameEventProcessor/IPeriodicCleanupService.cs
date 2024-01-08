using Polly;
using Wordle.Commands;

namespace Wordle.Apps.GameEventProcessor;

public interface IPeriodicCleanupService
{
    // this will be run occasionally to automatically force-close any sessions that have fallen through the event
    // processing system (in the case of a catastrophic failure).
    Task RunAsync(CancellationToken token);
}