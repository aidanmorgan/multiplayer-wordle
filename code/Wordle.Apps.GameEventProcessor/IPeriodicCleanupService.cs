using Polly;
using Wordle.Commands;

namespace Wordle.Apps.GameEventProcessor;

public interface IPeriodicCleanupService
{
    Task RunAsync(CancellationToken token);
}