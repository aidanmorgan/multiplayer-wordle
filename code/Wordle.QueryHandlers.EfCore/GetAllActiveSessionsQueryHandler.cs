using Dapper;
using MediatR;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetAllActiveSessionsQueryHandler : IRequestHandler<GetAllActiveSessionsQuery, List<ActiveSessionResult>>
{
    private readonly WordleEfCoreSettings _context;

    public GetAllActiveSessionsQueryHandler(WordleEfCoreSettings settings)
    {
        _context = settings;
    }

    public async Task<List<ActiveSessionResult>> Handle(GetAllActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _context.Connection.QueryAsync<ActiveSessionResult>(
            @"SELECT session.id as sessionid, session.version as sessionversion, session.activeroundend as roundexpiry, round.id as roundid, round.version as roundversion
                  FROM sessions session 
                  LEFT JOIN rounds round ON session.activeroundid = round.id 
                  WHERE session.state = @state AND session.activeroundend IS NOT NULL AND session.activeroundid IS NOT NULL",
            new
            {
                State = SessionState.ACTIVE
            });

        return sessions.ToList();
    }
}

