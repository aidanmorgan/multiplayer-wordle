using Wordle.Model;
using Wordle.Queries;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public interface IBoardStorage
{
    Task<string?> StoreBoard(Session session, List<Round> rounds, RenderOutput svg,
        Stream boardStream, CancellationToken token);
}