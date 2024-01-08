namespace Wordle.Apps.GameEventProcessor;

public interface IInitialisationService
{
    // used to enqueue any active sessions into the delayed processing queue in the case that we've had a complete
    // broker failure and we need to recover. Is only run once at the startup of each GameEventProcessor
    public Task<bool> RunAsync(CancellationToken token);
}