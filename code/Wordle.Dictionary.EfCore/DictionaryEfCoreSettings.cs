using Npgsql;

namespace Wordle.Dictionary.EfCore;

public class DictionaryEfCoreSettings
{
    public DictionaryEfCoreSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public DictionaryContext Context => new DictionaryContext(ConnectionString);
    
    public NpgsqlConnection Connection => new NpgsqlConnection(ConnectionString);
    
    public string ConnectionString { get; init; }
}