using System.Data;
using Dapper;
using Npgsql;
using Wordle.Model;

namespace Wordle.EfCore;

public class WordleEfCoreSettings
{
    public string ConnectionString { get; init; }

    public WordleContext DbContext => new WordleContext(ConnectionString);
    public NpgsqlConnection Connection => new NpgsqlConnection(ConnectionString);

    static WordleEfCoreSettings()
    {
        SqlMapper.AddTypeHandler(new StringListTypeHandler<List<string>>());
        SqlMapper.AddTypeHandler(new LetterStateListTypeHandler<List<LetterState>>());
    }

    public WordleEfCoreSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }
}

public class StringListTypeHandler<T> : SqlMapper.TypeHandler<List<string>>
{
    public override List<string> Parse(object value)
    {
        return ((string[])value).ToList();
    }

    public override void SetValue(IDbDataParameter parameter, List<string>? value)
    {
        parameter.Value = value.ToArray();
    }
}

public class LetterStateListTypeHandler<T> : SqlMapper.TypeHandler<List<LetterState>>
{
    public override List<LetterState> Parse(object value)
    {
        return ((int[])value).Select(x => (LetterState)x).ToList();
    }

    public override void SetValue(IDbDataParameter parameter, List<LetterState>? value)
    {
        parameter.Value = value.Select(x => (int)x).ToArray();
    }
}
