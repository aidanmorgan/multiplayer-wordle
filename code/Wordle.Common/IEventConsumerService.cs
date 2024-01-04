namespace Wordle.Common;

public interface IEventConsumerService
{
    ManualResetEventSlim ReadySignal { get; }
    
    Task RunAsync(CancellationToken token);
}