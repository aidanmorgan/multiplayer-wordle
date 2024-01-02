using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, SessionQueryResult?>
{
    private readonly WordleEfCoreSettings _context;

    public GetSessionByIdQueryHandler(WordleEfCoreSettings context)
    {
        _context = context;
    }

    public async Task<SessionQueryResult?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = (await _context.Connection.QueryAsync<Session>(
            "SELECT * FROM sessions WHERE id = @id",
            new
            {
                Id = request.Id
            })).FirstOrDefault();
        
        var result = new SessionQueryResult();
 
        if (session == null)
        {
            return null;
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
            result.Rounds = (await _context.Connection.QueryAsync<Round>(
                "SELECT * FROM rounds WHERE sessionid = @sessionId",
                new
                {
                    SessionId = request.Id
                })).ToList();
        }

        if (request.IncludeOptions)
        {
            result.Options = (await _context.Connection.QueryAsync<Options>(
                "SELECT * FROM options WHERE sessionId = @sessionId AND tenantid IS NULL",
                new
                {
                    SessionId = request.Id
                })).First();
        }

        return result;
    }
}