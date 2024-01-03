using Wordle.Common;

namespace Wordle.Apps.BoardGenerator.Impl;

public class LocalDiskStorage : IBoardStorage
{
    public static readonly Func<string, Guid, Guid, string> DefaultPathGenerator = (b, s, r) => Path.Combine(b, s.ToString());
    public static readonly Func<Guid, Guid, string> DefaultFileGenerator = (s, r) => $"{r}.png";

    private static readonly ReaderWriterLock FileSystemLock = new ReaderWriterLock();

    private static readonly Action<string> CreateBaseDirectory = Actions.callOnlyOnce<string>((x) =>
    {
        if (!Path.Exists(x))
        {
            Directory.CreateDirectory(x);
        }
    });
    
    private readonly string _baseDirectory;
    private readonly Func<string, Guid, Guid, string> _pathGenerator;
    private readonly Func<Guid, Guid, string> _fileNameGenerator;


    public LocalDiskStorage(string baseDirectory, Func<string, Guid, Guid, string>? pathGenerator = null, Func<Guid, Guid, string>? fileGenerator = null)
    {
        _baseDirectory = baseDirectory;
        _pathGenerator = pathGenerator ?? DefaultPathGenerator;
        _fileNameGenerator = fileGenerator ?? DefaultFileGenerator;
    }

    public async Task<string> StoreBoard(Guid sessionId, Guid roundId, Stream boardStream, CancellationToken token)
    {
        CreateBaseDirectory(_baseDirectory);
        
        var sessionDirectory = Path.Combine(_baseDirectory, _pathGenerator(_baseDirectory, sessionId, roundId));
        if (sessionDirectory != _baseDirectory)
        {
            Directory.CreateDirectory(sessionDirectory);
        }

        var file = Path.Combine(sessionDirectory, _fileNameGenerator(sessionId, roundId));

        // this is an attempt to protect from file locking issues, especailly in the case of writing to the
        // same location on the file system.
        try
        {
            FileSystemLock.AcquireWriterLock(TimeSpan.FromSeconds(5));
            await using var fileStream = File.Create(file);
            await boardStream.CopyToAsync(fileStream, token);
            return file;
        }
        finally
        {
            FileSystemLock.ReleaseWriterLock();
        }
    }
}