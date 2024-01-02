using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EntityFramework;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EntityFramework;

public class GetOptionsForTenantQueryHandler:  IRequestHandler<GetOptionsForTenantQuery, Options?>
{
    private readonly WordleContext _context;

    public GetOptionsForTenantQueryHandler(WordleContext context)
    {
        _context = context;
    }


    public Task<Options?> Handle(GetOptionsForTenantQuery request, CancellationToken cancellationToken)
    {
        var tenantId = $"{request.TenantType}#{request.TenantName}";
        
        var options = _context
            .Options
            .FromSql($"SELECT * FROM options WHERE tenantid = '{tenantId}'")
            .ToList()
            .FirstOrDefault((Options)null);

        return Task.FromResult(options);
    }
}