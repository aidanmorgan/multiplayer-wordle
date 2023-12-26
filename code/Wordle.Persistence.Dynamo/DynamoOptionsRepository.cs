using Wordle.Model;

namespace Wordle.Persistence.Dynamo;

public class DynamoOptionsRepository : DynamoRepositoryImpl<Options>, IOptionsRepository, IDynamoRepository<Options>
{
    public DynamoOptionsRepository(TableMapper<Options> tableMapper) : base(tableMapper)
    {
    }

    public Task AddAsync(string tenantId, Options o)
    {
        o.TenantId = tenantId;
        return base.AddAsync(o);
    }

    public Task AddAsync(Session s, Options o)
    {
        o.SessionId = s.CreateSessionIdString();
        return base.AddAsync(o);
    }

    public new Task AddAsync(Options o)
    {
        throw new NotImplementedException();
    }
}