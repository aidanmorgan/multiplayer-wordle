using Microsoft.EntityFrameworkCore;

namespace Wordle.Persistence.EntityFramework;

public class EfRepository<T> : IRepository<T> where T : class, new()
{
    private readonly DbSet<T> _context;

    public EfRepository(DbSet<T> context)
    {
        _context = context;
    }

    public async Task AddAsync(T val)
    {
        await _context.AddAsync(val);
    }

    public Task UpdateAsync(T val)
    {
        _context.Update(val);
        return Task.CompletedTask;
    }
}