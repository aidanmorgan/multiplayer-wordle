using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Wordle.Common;

public static class GuidExtensions
{

    public static string ToSessionId(this Guid id)
    {
        return $"{IIdConstants.SessionIdPrefix}{id:D}";
    }

    public static bool IsSessionId(this string id)
    {
        return id.StartsWith(IIdConstants.SessionIdPrefix);
    }
    
    public static string ToTenantId(this Guid id)
    {
        return $"{IIdConstants.TenantIdPrefix}{id:D}";
    }

    public static bool IsTenantId(this string id)
    {
        return id.StartsWith(IIdConstants.TenantIdPrefix);
    }

    public static Guid FromSessionId(this string id)
    {
        return ParseGuid(id, IIdConstants.SessionIdPrefix);
    }

    public static string ToRoundId(this Guid id)
    {
        return $"{IIdConstants.RoundIdPrefix}{id:D}";
    }

    public static Guid FromRoundId(this string id)
    {
        return ParseGuid(id, IIdConstants.RoundIdPrefix);
    }

    public static string ToOptionsId(this Guid id)
    {
        return $"{IIdConstants.OptionsIdPrefix}{id:D}";
    }

    public static Guid FromOptionsId(this string id)
    {
        return ParseGuid(id, IIdConstants.OptionsIdPrefix);
    }

    public static string ToGuessId(this Guid id)
    {
        return $"{IIdConstants.GuessIdPrefix}{id:D}";
    }

    public static Guid FromGuessId(this string id)
    {
        return ParseGuid(id, IIdConstants.GuessIdPrefix);
    }
    
    public static Guid FromTenantId(this string id)
    {
        return ParseGuid(id, IIdConstants.TenantIdPrefix);
    }    


    private static Guid ParseGuid(string input, string prefix)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Provided id string is null or empty.", nameof(input));
        }

        if (!input.StartsWith(prefix))
        {
            throw new ArgumentException("Provided id does not contain the correct prefix.", nameof(input));
        }

        var content = input.Substring(prefix.Length);
        return Guid.Parse(content);;
    }
}