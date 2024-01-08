using Microsoft.EntityFrameworkCore;
using Wordle.Model;

namespace Wordle.Persistence.EfCore;

public class EfSessionRepository : ISessionRepository
{
    private DbSet<Session> _sessions;

    public EfSessionRepository(DbSet<Session> sessions)
    {
        _sessions = sessions;
    }

    public Task AddAsync(Session val)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Session val)
    {
        val.Version += 1;
        
        _sessions.Update(val);
        return Task.CompletedTask;
    }

    public async Task AddAsync(string tenantId, Session s)
    {
        s.Tenant = tenantId;
        await _sessions.AddAsync(s);
    }
}