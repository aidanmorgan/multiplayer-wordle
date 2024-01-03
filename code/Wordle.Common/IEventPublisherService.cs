using Wordle.Events;

namespace Wordle.Common;

public interface IEventPublisherService
{
    Task Publish(IEvent ev, CancellationToken tk);
    Task RunAsync(CancellationToken token);
}