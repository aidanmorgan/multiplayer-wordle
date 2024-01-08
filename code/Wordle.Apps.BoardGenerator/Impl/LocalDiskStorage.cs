using Microsoft.Extensions.Logging;
using Wordle.Common;
using Wordle.Model;
using Wordle.Queries;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator.Impl;

public class LocalDiskStorage : IBoardStorage
{
    public static readonly Func<string, Session, List<Round>, string> DefaultPathGenerator = (b, s, r) => b;
    public static readonly Func<Session, List<Round>, string> DefaultFileGenerator = (s, r) => $"{s.Id}.svg";
    
    private static readonly Action<string> CreateBaseDirectory = Actions.callOnlyOnce<string>((x) =>
    {
        if (!Path.Exists(x))
        {
            Directory.CreateDirectory(x);
        }
    });
    
    private readonly string _baseDirectory;
    private readonly Func<string, Session, List<Round>, string> _pathGenerator;
    private readonly Func<Session, List<Round>, string> _fileNameGenerator;
    private readonly ILogger<LocalDiskStorage> _logger;


    public LocalDiskStorage(string baseDirectory, ILogger<LocalDiskStorage> logger,
        Func<string, Session, List<Round>, string>? pathGenerator = null, 
        Func<Session, List<Round>, string>? fileGenerator = null)
    {
        _baseDirectory = baseDirectory;
        _logger = logger;
        _pathGenerator = pathGenerator ?? DefaultPathGenerator;
        _fileNameGenerator = fileGenerator ?? DefaultFileGenerator;
    }

    public async Task<string?> StoreBoard(Session session, List<Round> rounds, RenderOutput svg, Stream stream, CancellationToken token)
    {
        CreateBaseDirectory(_baseDirectory);
        
        var sessionDirectory = Path.Combine(_baseDirectory, _pathGenerator(_baseDirectory, session, rounds));
        if (sessionDirectory != _baseDirectory)
        {
            Directory.CreateDirectory(sessionDirectory);
        }

        var file = Path.Combine(sessionDirectory, _fileNameGenerator(session, rounds));

        try
        {
            // need to be able to overwrite
            await using var fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            await stream.CopyToAsync(fileStream, token);
            await fileStream.FlushAsync(token);
            
            return file;
        }
        catch(IOException x)
        {
            _logger.LogWarning(x, "Exception thrown attempting to write board to {File}", file);
            return null;
        }
    }
}