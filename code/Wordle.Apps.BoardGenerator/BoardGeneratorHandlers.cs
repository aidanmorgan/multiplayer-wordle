using MediatR;
using Microsoft.Extensions.Logging;
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
            _logger.LogError("Attempting to generate for Session {SessionId}, but could not load", ev.VersionedSession);
            return;
        }

        // we will hit a fun ordering problem here, if the board processor runs after the end session has created a new round
        // then we will pick up the new round, so filter out any rounds that were created AFTER the event was spawned to
        // keep it to the same time as when the event was generated
        var rounds = session
            .Rounds
            .Where(x => x.CreatedAt < ev.Timestamp)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        MemoryStream stream = null;
        using (stream = new MemoryStream())
        {
            _renderer.Render(
                rounds.Select(x => new DisplayWord(x.Guess, x.Result)).ToList() ?? new List<DisplayWord>(),
                null, RenderOutput.Svg, stream);

            // need to actually push the data to the stream so we can read it back.
            await stream.FlushAsync(token);
            stream.Close();
        }

        using var outputStream = new MemoryStream(stream.GetBuffer());
        var location = await _storage.StoreBoard(session.Session, rounds, RenderOutput.Svg, outputStream, token);

        _logger.LogInformation("Board for {Session} and {Round} written to {Location}", ev.VersionedSession, ev.VersionedRound, location);
    }
}