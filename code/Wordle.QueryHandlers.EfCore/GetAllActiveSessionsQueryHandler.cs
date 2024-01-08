using Dapper;
using MediatR;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetAllActiveSessionsQueryHandler : IRequestHandler<GetAllActiveSessionsQuery, List<SessionAndRound>>
{
    private readonly WordleEfCoreSettings _context;

    public GetAllActiveSessionsQueryHandler(WordleEfCoreSettings settings)
    {
        _context = settings;
    }

    public async Task<List<SessionAndRound>> Handle(GetAllActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _context.Connection.QueryAsync<dynamic>(
            @"SELECT session.id as sessionid, session.version as sessionversion, session.activeroundend as roundend, round.id as roundid, round.version as roundversion
                  FROM sessions session 
                  LEFT JOIN rounds round ON session.activeroundid = round.id 
                  WHERE session.state = @state",
            new
            {
                State = SessionState.ACTIVE
            });

        var result = sessions.ToList().Select(x => 
            new SessionAndRound()
            {
                Session = new VersionId()
                {
                    Id = x.sessionid,
                    Version = x.sessionversion
                },
                Round = new VersionId()
                {
                    Id = x.roundid,
                    Version = x.roundversion
                },
                RoundExpiry = x.roundend
            });

        return result.ToList();
    }
}