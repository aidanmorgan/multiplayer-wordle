using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public interface IBoardStorage
{
    Task<string> StoreBoard(Guid sessionId, Guid roundId, RenderOutput svg, Stream boardStream, CancellationToken token);
}