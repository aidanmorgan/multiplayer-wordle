using Wordle.Model;

namespace Wordle.Persistence;

public interface ISessionRepository : IRepository<Session>
{
    public Task AddAsync(string tenantId, Session s);
}