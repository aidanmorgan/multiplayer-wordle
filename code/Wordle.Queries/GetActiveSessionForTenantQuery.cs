using MediatR;

namespace Wordle.Queries;

public class GetActiveSessionForTenantQuery : IRequest<Guid?>
{
    public string TenantType { get; private set; }
    public string TenantName { get; private set; }

    public GetActiveSessionForTenantQuery(string type, string tenantName)
    {
        TenantType = type;
        TenantName = tenantName;
    }

    public GetActiveSessionForTenantQuery(string tenantString)
    {
        var split = tenantString.Split("#");

        if (split.Length != 2)
        {
            throw new ArgumentException();
        }

        TenantType = split[0];
        TenantName = split[1];
    }
}