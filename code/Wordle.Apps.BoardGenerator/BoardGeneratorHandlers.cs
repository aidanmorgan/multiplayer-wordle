using Amazon.S3;
using MediatR;
using Microsoft.Extensions.Logging;
using Wordle.Apps.Common;
using Wordle.Events;
using Wordle.Queries;
using Wordle.Render;

namespace Wordle.Apps.BoardGenerator;

public class BoardGeneratorHandlers : INotificationHandler<RoundEnded>
{
    private readonly IMediator _mediator;
    private readonly IRenderer _renderer;
    private readonly IBoardStorage _storage;
    private readonly ILogger<BoardGeneratorHandlers> _logger;

    public BoardGeneratorHandlers(IMediator mediator, IRenderer renderer, IBoardStorage storage, ILogger<BoardGeneratorHandlers> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _storage = storage;
        _renderer = renderer;
    }

    public async Task Handle(RoundEnded ev, CancellationToken token)
    {
        var session = await _mediator.Send(new GetSessionByIdQuery(ev.SessionId, ev.SessionVersion), token);
        if (session == null)
        {
            _logger.LogError("Attempting to generate for Session {SessionId}, but could not load", ev.SessionId);
            return;
        }
                            
        var round = session?.Rounds.FirstOrDefault(x => x.Id == ev.RoundId);
        if (round == null)
        {
            _logger.LogError("Attempting to generate for Session {SessionId} and Round {RoundId} but Round could not be found", ev.SessionId, ev.RoundId);
            return;
        }

        using var stream = new MemoryStream();
        _renderer.Render(session?.Rounds.Select(x => new DisplayWord(x.Guess, x.Result)).ToList() ?? new List<DisplayWord>(), null, RenderOutput.Svg, stream);

        var location = await _storage.StoreBoard(ev.SessionId, ev.RoundId, RenderOutput.Svg, stream, token);
        _logger.LogInformation("Uploaded board image: {ImageFilename}", location);
    } 
}