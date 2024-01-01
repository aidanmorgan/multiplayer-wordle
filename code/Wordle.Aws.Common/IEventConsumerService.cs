namespace Wordle.Aws.Common;

public interface IEventConsumerService
{
    Task RunAsync(CancellationToken token);
}