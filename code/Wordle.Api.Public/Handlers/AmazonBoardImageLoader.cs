using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Wordle.Apps.Common;
using Wordle.Queries;

namespace Wordle.Api.Public.Handlers;

public class AmazonBoardImageLoader : IBoardImageLoader
{
    private IAmazonS3 _s3;

    public AmazonBoardImageLoader(IAmazonS3 s3)
    {
        _s3 = s3;
    }

    public async Task<ActionResult> GetImageForBoard(SessionQueryResult res)
    {
        // if there is no active round, or we can't find what we think the currently active round is
        // then just default to returning an empty grid which is better than nothing.
        if (res.Rounds.Count <= 1)
        {
            return new RedirectResult(EnvironmentVariables.EmptyGridImageUrl, false);
        }
        
        var filename = $"boards/{res.Session.Id}.{res.Rounds.Count}.png";

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