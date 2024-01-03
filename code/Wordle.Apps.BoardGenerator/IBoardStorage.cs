namespace Wordle.Apps.BoardGenerator;

public interface IBoardStorage
{
    Task<string> StoreBoard(Guid sessionId, Guid roundId, Stream boardStream, CancellationToken token);
}