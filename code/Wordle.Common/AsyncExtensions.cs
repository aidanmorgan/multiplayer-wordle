namespace Wordle.Common;

public static class AsyncExtensions
{
    public static Task WaitAll(this IEnumerable<Task> tasks)
    {
        Task.WaitAll(tasks.ToArray());
        return Task.CompletedTask;
    }
    
    public static Task<IEnumerable<T>> WaitAll<T>(this IEnumerable<Task<T>> tasks)
    {
        Task.WaitAll(tasks.ToArray());
        return Task.FromResult(tasks.Select(x => x.Result));
    }
}