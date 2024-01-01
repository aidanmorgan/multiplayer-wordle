using Amazon.S3;
using MediatR;
using Wordle.Apps.Common;
using Wordle.Events;
using Wordle.Logger;
using Wordle.Queries;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class BoardGeneratorHandlers : INotificationHandler<RoundEnded>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IAmazonS3 _s3;
    private readonly IRenderer _renderer;

    public BoardGeneratorHandlers(IMediator mediator, IRenderer renderer, IAmazonS3 s3, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger;
        _s3 = s3;
        _renderer = renderer;
    }

    public async Task Handle(RoundEnded ev, CancellationToken token)
    {
        var session = await _mediator.Send(new GetSessionByIdQuery(ev.SessionId), token);
        if (session == null)
        {
            _logger.Log($"Attempting to generate for Session {ev.SessionId}, but could not load.");
            return;
        }
                            
        var round = session?.Rounds.FirstOrDefault(x => x.Id == ev.RoundId);
        if (round == null)
        {
            _logger.Log($"Attempting to generate for Session {ev.SessionId} and Round {ev.RoundId} but Round could not be found.");
            return;
        }

        using var stream = new MemoryStream();
        _renderer.Render(session?.Rounds.Select(x => new DisplayWord(x.Guess, x.Result)).ToList() ?? new List<DisplayWord>(), null, stream);

        var filename = $"boards/{ev.SessionId}.{ev.RoundId}.png";
                            
        await _s3.UploadObjectFromStreamAsync(EnvironmentVariables.BoardBucketName, filename, stream, new Dictionary<string, object>(), token);
                            
        _logger.Log($"Uploaded board image: {filename}");
        
    } 
}