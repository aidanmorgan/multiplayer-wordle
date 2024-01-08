using Microsoft.AspNetCore.Mvc;
using Wordle.Apps.Common;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Api.Public.Handlers;

public class LocalDiskImageLoader : IBoardImageLoader
{
    public static readonly Func<string, Session, List<Round>, string> DefaultPathGenerator = (b, s, r) => b;
    public static readonly Func<Session, List<Round>, string> DefaultFileGenerator = (s, r) => $"{s.Id}.svg";
    
    private readonly string _baseDirectory;
    private readonly Func<string, Session, List<Round>, string> _pathGenerator;
    private readonly Func<Session, List<Round>, string> _fileGenerator;

    public LocalDiskImageLoader(string baseDirectory, 
        Func<string, Session, List<Round>, string>? pathGenerator = null,
        Func<Session, List<Round>, string>? fileGenerator = null)
    {
        _baseDirectory = baseDirectory;
        _pathGenerator = pathGenerator ?? DefaultPathGenerator;
        _fileGenerator = fileGenerator ?? DefaultFileGenerator;
    }

    public async Task<ActionResult> GetImageForBoard(SessionQueryResult res)
    {
        // if there is no active round, or we can't find what we think the currently active round is
        // then just default to returning an empty grid which is better than nothing.
        if (res.Rounds.Count <= 1)
        {
            return new RedirectResult(EnvironmentVariables.EmptyGridImageUrl, false);
        }

        var baseDirectory = _pathGenerator(_baseDirectory, res.Session, res.Rounds);
        
        var filePath = Path.Combine(baseDirectory, _fileGenerator(res.Session, res.Rounds));
        if (!Path.Exists(filePath))
        {
            return new NotFoundResult();
        }
        
        await using var fileStream = File.Open(filePath, FileMode.Open);
        return new FileStreamResult(fileStream, "image/svg+xml");
    }
}