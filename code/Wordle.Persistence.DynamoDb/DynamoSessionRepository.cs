using Wordle.Model;

namespace Wordle.Persistence.DynamoDb;

public class DynamoSessionRepository: DynamoRepositoryImpl<Session>, IDynamoRepository<Session>, ISessionRepository
{
    public DynamoSessionRepository(TableMapper<Session> tableMapper) : base(tableMapper)
    {
    }

    public Task AddAsync(string tenantId, Session s)
    {
        s.Tenant = tenantId;
        return base.AddAsync(s);
    }

    public Task AddAsync(Session s)
    {
        throw new NotImplementedException();
    }
}