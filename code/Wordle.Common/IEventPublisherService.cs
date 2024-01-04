using Wordle.Events;

namespace Wordle.Common;

public interface IEventPublisherService
{
    ManualResetEventSlim ReadySignal { get; }

    Task Publish(IEvent ev, CancellationToken tk);
    Task RunAsync(CancellationToken token);
}