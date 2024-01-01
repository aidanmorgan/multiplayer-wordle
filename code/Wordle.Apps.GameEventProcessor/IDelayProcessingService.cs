using Amazon.Runtime.Internal.Util;
using Hangfire.Logging;
using Nito.AsyncEx;
using Wordle.Apps.Common;

namespace Wordle.Apps.GameEventProcessor;

public interface IDelayProcessingService
{
    Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken token);

    Task RunAsync(CancellationToken token);
}
