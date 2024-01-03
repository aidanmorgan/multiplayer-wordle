using Wordle.Common;

namespace Wordle.Persistence.DynamoDb;

public class Tenant
{
    public static string CreateTenantId(string type, string name)
    {
        return $"{IIdConstants.TenantIdPrefix}{type}#{name}";
    }
}