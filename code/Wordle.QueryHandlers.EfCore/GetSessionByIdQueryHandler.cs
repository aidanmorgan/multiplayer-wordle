using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EntityFramework;

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, SessionQueryResult?>
{
    private readonly WordleContext _context;

    public GetSessionByIdQueryHandler(WordleContext context)
    {
        _context = context;
    }

    public Task<SessionQueryResult?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var result = new SessionQueryResult();

        var sessionId = $"{request.Id}";

        var session = _context.Sessions
            .FromSql($"SELECT * FROM sessions WHERE id = '{sessionId}'")
            .AsEnumerable()
            .FirstOrDefault((Session?)null);

        if (session == null)
        {
            return Task.FromResult((SessionQueryResult)null);
        }
        else
        {
            result.Session = session;
        }

        if (!request.IncludeWord)
        {
            result.Session.Word = null;
        }

        if (request.IncludeRounds)
        {
            result.Rounds = _context.Rounds.Where(x => x.SessionId == request.Id).ToList();
        }

        if (request.IncludeOptions)
        {
            result.Options =
                _context.Options.Where(x => x.SessionId == request.Id && x.TenantId == null).FirstOrDefault((Options?)null);

        }

        return Task.FromResult((SessionQueryResult?)result);
    }
}