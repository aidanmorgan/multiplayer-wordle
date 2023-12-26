namespace Wordle.Persistence;

public interface IRepository<T> where T : class, new()
{
    Task AddAsync(T val);
    Task UpdateAsync(T val);
}