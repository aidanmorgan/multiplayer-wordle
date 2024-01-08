using MediatR;
using Wordle.Model;

namespace Wordle.Queries;

public class GetActiveSessionForTenantQuery : IRequest<VersionId<Session>?>
{
    public string TenantName { get; private set; }

    public GetActiveSessionForTenantQuery(string tenantName)
    {
        TenantName = tenantName;
    }
}