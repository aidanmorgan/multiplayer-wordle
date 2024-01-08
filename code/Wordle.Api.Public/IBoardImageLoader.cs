using Microsoft.AspNetCore.Mvc;
using Wordle.Queries;

namespace Wordle.Api.Public;

public interface IBoardImageLoader
{
    public Task<ActionResult> GetImageForBoard(SessionQueryResult res);
}