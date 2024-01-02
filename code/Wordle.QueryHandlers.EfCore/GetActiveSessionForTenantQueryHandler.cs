using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.Common;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EntityFramework;

public class GetActiveSessionForTenantQueryHandler : IRequestHandler<GetActiveSessionForTenantQuery, Guid?>
{
    private readonly WordleContext _context;

    public GetActiveSessionForTenantQueryHandler(WordleContext context)
    {
        _context = context;
    }

    public Task<Guid?> Handle(GetActiveSessionForTenantQuery request, CancellationToken cancellationToken)
    {
        var tenantId = $"{request.TenantType}#{request.TenantName}";

        var sessions = _context
            .Sessions
            .FromSql($"SELECT * FROM sessions WHERE state = {SessionState.ACTIVE} AND tenant = '{tenantId}' ORDER BY createdat DESC")
            .ToList();
            
        
        return Task.FromResult(sessions.FirstOrDefault()?.Id);
    }
}