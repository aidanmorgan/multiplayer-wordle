using Amazon.S3;
using Amazon.S3.Model;
using Wordle.Apps.Common;

namespace Wordle.Apps.BoardGenerator.Impl;

public class AwsBoardStorage : IBoardStorage
{
    private readonly IAmazonS3 _s3;

    public AwsBoardStorage(IAmazonS3 s3)
    {
        _s3 = s3;
    }

    public async Task<string> StoreBoard(Guid sessionId, Guid roundId, Stream stream, CancellationToken token)
    {
        var filename = $"boards/{sessionId}.{roundId}.png";
        await _s3.UploadObjectFromStreamAsync(EnvironmentVariables.BoardBucketName, filename, stream, new Dictionary<string, object>(), token);
        return filename;
    }
}