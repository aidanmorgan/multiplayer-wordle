using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public class GetOptionsForTenantQuery : IRequest<Options?>
{
    public string TenantName { get; set; }

    public GetOptionsForTenantQuery(string tenantName)
    {
        TenantName = tenantName;
    }
}