namespace Wordle.Common;

public interface IEventConsumerService
{
    Task RunAsync(CancellationToken token);
}