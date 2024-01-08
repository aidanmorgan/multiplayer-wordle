using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetActiveSessionForTenantQueryHandler : IRequestHandler<GetActiveSessionForTenantQuery, VersionId<Session>?>
{
    private readonly WordleEfCoreSettings _context;

    public GetActiveSessionForTenantQueryHandler(WordleEfCoreSettings settings)
    {
        _context = settings;
    }

    public async Task<VersionId<Session>?> Handle(GetActiveSessionForTenantQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Connection.QueryAsync<Session>(
            "SELECT * FROM sessions WHERE state = @state AND tenant = @tenant ORDER BY createdat DESC", new
            {
                State = SessionState.ACTIVE,
                Tenant = request.TenantName
            });

        var entry = result.FirstOrDefault();
        if (entry == null)
        {
            return null;
        }
        else
        {
            return new VersionId<Session>()
            {
                Id = entry.Id,
                Version = entry.Version
            };
        }
    }
}