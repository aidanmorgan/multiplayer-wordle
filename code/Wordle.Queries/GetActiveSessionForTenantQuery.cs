using MediatR;

namespace Wordle.Queries;

public class GetActiveSessionForTenantQuery : IRequest<VersionId?>
{
    public string TenantName { get; private set; }

    public GetActiveSessionForTenantQuery(string tenantName)
    {
        TenantName = tenantName;
    }
}