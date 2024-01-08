using Microsoft.EntityFrameworkCore;
using Wordle.Model;

namespace Wordle.Persistence.EfCore;

public class EfRepository<T> : IRepository<T> where T : class, new()
{
    private readonly DbSet<T> _context;
    private readonly bool _allowUpdate;

    public EfRepository(DbSet<T> context, bool allowUpdate = true)
    {
        _context = context;
        _allowUpdate = allowUpdate;
    }

    public async Task AddAsync(T val)
    {
        await _context.AddAsync(val);
    }

    public Task UpdateAsync(T val)
    {
        if (!_allowUpdate)
        {
            throw new NotImplementedException();
        }
        
        var versionable = val as IVersioned;
        if (versionable != null)
        {
            versionable.Version = versionable.Version + 1;
        }
        
        _context.Update(val);
        return Task.CompletedTask;
    }
}