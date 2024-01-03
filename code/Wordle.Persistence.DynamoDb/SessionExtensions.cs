using Wordle.Common;
using Wordle.Model;

namespace Wordle.Persistence.DynamoDb;

public static class SessionExtensions
{
    public static string CreateSessionIdString(this Session s)
    {
        return $"{IIdConstants.SessionIdPrefix}{s.Id}";
    }
}