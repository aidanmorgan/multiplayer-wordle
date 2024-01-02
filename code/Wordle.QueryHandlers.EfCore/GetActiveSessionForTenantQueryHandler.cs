using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetActiveSessionForTenantQueryHandler : IRequestHandler<GetActiveSessionForTenantQuery, Guid?>
{
    private readonly WordleEfCoreSettings _context;

    public GetActiveSessionForTenantQueryHandler(WordleEfCoreSettings settings)
    {
        _context = settings;
    }

    public async Task<Guid?> Handle(GetActiveSessionForTenantQuery request, CancellationToken cancellationToken)
    {
        var tenantId = $"{request.TenantType}#{request.TenantName}";

        var result = await _context.Connection.QueryAsync<Session>(
            "SELECT * FROM sessions WHERE state = @state AND tenant = @tenant ORDER BY createdat DESC", new
            {
                State = SessionState.ACTIVE,
                Tenant = tenantId
            });

        return result.FirstOrDefault()?.Id;
    }
}