using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Wordle.Apps.Common;

namespace Wordle.Api.Public;

public interface IBoardImageHandler
{
    public Task<ActionResult> GetImageForBoard(Guid sessionId, Guid? roundId, int index);
}

public class AmazonBoardImageHander : IBoardImageHandler
{
    private IAmazonS3 _s3;

    public AmazonBoardImageHander(IAmazonS3 s3)
    {
        _s3 = s3;
    }

    public async Task<ActionResult> GetImageForBoard(Guid sessionId, Guid? roundId, int roundIndex)
    {
        // if there is no active round, or we can't find what we think the currently active round is
        // then just default to returning an empty grid which is better than nothing.
        if (!roundId.HasValue || roundIndex <= 0)
        {
            return new RedirectResult(EnvironmentVariables.EmptyGridImageUrl, false);
        }
        
        var filename = $"boards/{sessionId}.{roundId}.png";

        var expiryUrlRequest = new GetPreSignedUrlRequest()
        {
            BucketName = EnvironmentVariables.BoardBucketName,
            Key = filename,
            Expires = DateTime.UtcNow.AddDays(10)
        };
        
        var url = await _s3.GetPreSignedURLAsync(expiryUrlRequest);

        return new RedirectResult(url, true);
    }
}

public class LocalDiskImageHandler : IBoardImageHandler
{
    public static readonly Func<Guid, Guid, string> DefaultPathGenerator = (s, r) => $"{s}/{r}.png";
    
    private readonly string _baseDirectory;
    private readonly Func<Guid, Guid, string> _pathGenerator;

    public LocalDiskImageHandler(string baseDirectory, Func<Guid, Guid, string>? pathGenerator = null)
    {
        _baseDirectory = baseDirectory;
        _pathGenerator = pathGenerator ?? DefaultPathGenerator;
    }

    public async Task<ActionResult> GetImageForBoard(Guid sessionId, Guid? roundId, int roundIndex)
    {
        // if there is no active round, or we can't find what we think the currently active round is
        // then just default to returning an empty grid which is better than nothing.
        if (!roundId.HasValue || roundIndex <= 0)
        {
            return new RedirectResult(EnvironmentVariables.EmptyGridImageUrl, false);
        }
        
        var filePath = Path.Combine(_baseDirectory, _pathGenerator(sessionId, roundId.Value));

        if (!Path.Exists(filePath))
        {
            return new NotFoundResult();
        }
        
        await using var fileStream = File.Create(filePath);
        return new FileStreamResult(fileStream, "image/png");
    }
}