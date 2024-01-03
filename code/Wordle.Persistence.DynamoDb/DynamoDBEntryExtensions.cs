using Amazon.DynamoDBv2.DocumentModel;
using Wordle.Common;

namespace Wordle.Persistence.DynamoDb;

public static class DynamoDBEntryExtensions
{
    private static readonly string ListSeparator = "::";
    private static readonly int ListSeparatorLength = ListSeparator.Length;
    
    public static bool IsNull(this DynamoDBEntry entry)
    {
        return entry is DynamoDBNull;
    }
    
    public static Task<DynamoDBEntry> AsSessionIdAsync(this Guid id)
    {
        return MakeDynamoAsync(id.ToSessionId());
    }
    
    public static Task<DynamoDBEntry> AsNullableSessionIdAsync(this Guid? id)
    {
        if (id.HasValue)
        {
            return MakeDynamoAsync(((Guid)id).ToSessionId());
        }
        else
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }
    }
    
    
    public static Task<DynamoDBEntry> AsTenantIdAsync(this Guid id)
    {
        return MakeDynamoAsync(id.ToTenantId());
    }    

    public static Task<Guid> AsSessionIdAsync(this DynamoDBEntry id)
    {
        return Task.FromResult(id.AsString().FromSessionId());
    }
    
    public static Task<Guid> AsTenantIdAsync(this DynamoDBEntry id)
    {
        return Task.FromResult(id.AsString().FromTenantId());
    }

    public static Task<DynamoDBEntry> AsRoundIdAsync(this Guid id)
    {
        return MakeDynamoAsync(id.ToRoundId());
    }

    public static Task<Guid> AsRoundIdAsync(this DynamoDBEntry id)
    {
        return Task.FromResult(id.AsString().FromRoundId());
    }

    public static Task<DynamoDBEntry> AsOptionsIdAsync(this Guid id)
    {
        return MakeDynamoAsync(id.ToOptionsId());
    }

    public static Task<Guid> AsOptionsIdAsync(this DynamoDBEntry id)
    {
        return Task.FromResult(id.AsString().FromOptionsId());
    }

    public static Task<DynamoDBEntry> AsGuessIdAsync(this Guid id)
    {
        return MakeDynamoAsync(id.ToGuessId());
    }

    public static Task<Guid> AsGuessIdAsync(this DynamoDBEntry id)
    {
        return Task.FromResult(id.AsString().FromGuessId());
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync(this Guid id)
    {
        return Task.FromResult<DynamoDBEntry>(new Primitive($"{id}"));
    }
    
    public static Task<DynamoDBEntry> MakeDynamoAsync(this Guid? id)
    {
        if (id.HasValue && !id.Equals(Guid.Empty))
        {
            return MakeDynamoAsync(id.Value.ToString());
        }

        return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync(this Enum? e)
    {
        if (e == null)
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }
        
        return MakeDynamoAsync(e.ToString());
    }
    
    public static Task<DynamoDBEntry> MakeDynamoAsync(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }
        return Task.FromResult<DynamoDBEntry>(new Primitive(s));
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync(this int i)
    {
        return Task.FromResult<DynamoDBEntry>(new Primitive($"{i}", true));
    }
    
    public static Task<DynamoDBEntry> MakeDynamoAsync(this int? i)
    {
        if (!i.HasValue)
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }
        
        return Task.FromResult<DynamoDBEntry>(new Primitive($"{i}", true));
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync(this List<string>? list)
    {
        if (list == null || list.Count == 0)
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }

        return Task.FromResult<DynamoDBEntry>(new PrimitiveList()
        {
            Entries =  list.Select((t, i) => new Primitive($"{i}{ListSeparator}{t}")).ToList()
        });
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync(this DateTimeOffset? o)
    {
        if (!o.HasValue)
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }
        
        return Task.FromResult<DynamoDBEntry>(new Primitive(o.Value.ToString("o")));
    }
    
    public static Task<DynamoDBEntry> MakeDynamoAsync(this DateTimeOffset o)
    {
        return Task.FromResult<DynamoDBEntry>(new Primitive(o.ToString("o")));
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync<T>(this List<T>? val) where T : struct, Enum
    {
        if (val == null || val.Count == 0)
        {
            return Task.FromResult<DynamoDBEntry>(new DynamoDBNull());
        }

        return Task.FromResult<DynamoDBEntry>(new PrimitiveList(DynamoDBEntryType.String)
        {
            Entries = val.Select((t, i) => new Primitive($"{i}{ListSeparator}{t}")).ToList()
        });
    }

    public static Task<DateTimeOffset> AsDateTimeOffset(this DynamoDBEntry entry)
    {
        var str = entry.AsString();
        return Task.FromResult(DateTimeOffset.Parse(str));
    }
    
    public static Task<DateTimeOffset?> AsNullableDateTimeOffsetAsync(this DynamoDBEntry entry)
    {
        if (entry.IsNull())
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }
        
        var str = entry.AsString();
        
        if(string.IsNullOrEmpty(str))
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        return Task.FromResult<DateTimeOffset?>(DateTimeOffset.Parse(str));
    }

    public static Task<string> AsStringAsync(this DynamoDBEntry entry)
    {
        return entry.IsNull() ? Task.FromResult<string>(null) : Task.FromResult(entry.AsString());
    }

    public static Task<int> AsIntAsync(this DynamoDBEntry entry)
    {
        return Task.FromResult(entry.AsInt());
    }

    public static Task<Guid> AsGuidAsync(this DynamoDBEntry entry)
    {
        return Task.FromResult<Guid>(entry.AsGuid());
    }
    
    public static Task<Guid?> AsNullableGuidAsync(this DynamoDBEntry entry)
    {
        return Task.FromResult<Guid?>(entry.IsNull() ? null : entry.AsGuid());
    }

    public static Task<T> AsEnumAsync<T>(this DynamoDBEntry entry) where T : struct
    {
        if (entry.IsNull())
        {
            return Task.FromResult(default(T));
        }
        return Task.FromResult(Enum.Parse<T>(entry.AsString()));
    }

    public static Task<List<string>> AsStringListAsync(this DynamoDBEntry entry)
    {
        if (entry.IsNull())
        {
            return Task.FromResult(new List<string>());
        }

        var list = ((PrimitiveList)entry).AsListOfString();
        return Task.FromResult(list
            .Select(x => new Tuple<int,string>(
                int.Parse(x.Substring(0, x.IndexOf(ListSeparator, StringComparison.Ordinal))), 
                x.Substring(x.IndexOf(ListSeparator, StringComparison.Ordinal) + ListSeparatorLength)
            )).OrderBy(x => x.Item1)
            .Select(x => x.Item2).ToList());        
    }

    public static Task<List<T>> AsEnumListAsync<T>(this DynamoDBEntry entry) where T : struct
    {
        if (entry.IsNull())
        {
            return Task.FromResult(new List<T>());
        }
        
        var list = ((PrimitiveList)entry).AsListOfString();
        if (list.Count == 0)
        {
            return Task.FromResult(new List<T>());
        }
        
        return Task.FromResult(list
            .Select(x => new Tuple<int,T>(
                int.Parse(x.Substring(0, x.IndexOf(ListSeparator, StringComparison.Ordinal))), 
                Enum.Parse<T>(x.Substring(x.IndexOf(ListSeparator, StringComparison.Ordinal) + ListSeparatorLength))
            )).OrderBy(x => x.Item1)
            .Select(x => x.Item2).ToList());
    }

    public static Task<DynamoDBEntry> MakeDynamoAsync(this bool b)
    {
        return Task.FromResult<DynamoDBEntry>(new DynamoDBBool(b));
    }

    public static Task<bool> AsBooleanAsync(this DynamoDBEntry entry)
    {
        return Task.FromResult(entry.AsBoolean());
    }
}