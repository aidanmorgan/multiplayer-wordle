using MediatR;
using Wordle.Model;

namespace Queries;

public class GetOptionsForTenantQuery : IRequest<Options?>
{
    public string TenantType { get; set; }
    public string TenantName { get; set; }

    public GetOptionsForTenantQuery(string tenantType, string tenantName)
    {
        TenantType = tenantType;
        TenantName = tenantName;
    }
}