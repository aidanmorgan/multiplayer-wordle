using Microsoft.EntityFrameworkCore;
using Wordle.Model;

namespace Wordle.Persistence.EntityFramework;

public class EfOptionsRepository : IOptionsRepository
{
    private DbSet<Options> _options;

    public EfOptionsRepository(DbSet<Options> options)
    {
        _options = options;
    }

    public Task AddAsync(Options val)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Options val)
    {
        _options.Update(val);
        return Task.CompletedTask;
    }

    public async Task AddAsync(string tenantId, Options o)
    {
        o.TenantId = tenantId;
        o.SessionId = null;

        await _options.AddAsync(o);
    }

    public async Task AddAsync(Session s, Options o)
    {
        o.SessionId = s.Id;
        o.TenantId = null;
        
        await _options.AddAsync(o);
    }
}