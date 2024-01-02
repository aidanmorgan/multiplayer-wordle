using Amazon.Util.Internal;
using Microsoft.EntityFrameworkCore;
using Wordle.Dictionary.EfCore;

namespace DictionaryLoader;

public class PostgresInserter : Common
{
    public const int BATCH_SIZE = 200;
    private readonly string _connectionString;

    public PostgresInserter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task Run()
    {
        var context = new DictionaryContext(_connectionString);
        await context.Database.MigrateAsync();

        TYPES.ForEach(type =>
        {
            var words = ReadDictionary(type);

            var entries = words.Select(word => new Entry()
                {
                    Language = type,
                    WordLength = word.Length,
                    Word = word
                })
                .ToList();

            int count = 0;

            entries
                .Chunk(BATCH_SIZE)
                .ToList()
                .ForEach(x =>
                {
                    count += x.Length;
                    context.Words.BulkInsert(x);
                    Console.WriteLine($"Wrote {count} of {entries.Count} entries for {type}");
                });
        });
    }
}