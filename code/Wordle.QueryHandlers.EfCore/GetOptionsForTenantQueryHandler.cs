using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wordle.EfCore;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.QueryHandlers.EfCore;

public class GetOptionsForTenantQueryHandler:  IRequestHandler<GetOptionsForTenantQuery, Options?>
{
    private readonly WordleEfCoreSettings _context;

    public GetOptionsForTenantQueryHandler(WordleEfCoreSettings context)
    {
        _context = context;
    }


    public async Task<Options?> Handle(GetOptionsForTenantQuery request, CancellationToken cancellationToken)
    {
        var options = await _context.Connection.QueryAsync<Options>(
            "SELECT * FROM options WHERE tenantid = @tenantId AND sessionid IS NULL",
            new
            {
                TenantId = request.TenantName
            });
        

        return options.FirstOrDefault();
    }
}