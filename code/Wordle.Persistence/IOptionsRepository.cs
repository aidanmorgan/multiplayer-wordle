using Wordle.Model;

namespace Wordle.Persistence;

public interface IOptionsRepository : IRepository<Options>
{
    public Task AddAsync(string tenantId, Options o);
    public Task AddAsync(Session s, Options o);
}