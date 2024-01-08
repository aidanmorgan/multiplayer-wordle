namespace Wordle.Apps.GameEventProcessor;

// used to enqueue any active sessions into the delayed processing queue in the case that we've had a complete
// broker failure and we need to recover 
public interface IInitialisationService
{
    public Task<bool> RunAsync(CancellationToken token);
}