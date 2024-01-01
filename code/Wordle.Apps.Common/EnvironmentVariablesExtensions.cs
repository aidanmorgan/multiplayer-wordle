namespace Wordle.Apps.Common;

public static class EnvironmentVariablesExtensions
{
    public static void SetDefault(string key, string value)
    {
        var o = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(o))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}