using System.Reflection;

namespace Wordle.Apps.Common;

public  static class EnvironmentVariables
{
    
    public static readonly List<string> EnvironmentFiles =
    [
        ".development.env",
        ".sensitive.env"
    ];


    static EnvironmentVariables()
    {
        var assembly = Assembly.GetAssembly(typeof(EnvironmentVariables));
        foreach (var file in EnvironmentFiles)
        {
            using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{file}");
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line) || !line.Contains("="))
                {
                    continue;
                }

                var split = line.Split("=");
                Environment.SetEnvironmentVariable(split[0], split[1]);
            }
        }
    }
    
    public static string EventQueueUrl => Environment.GetEnvironmentVariable("EVENT_QUEUE_URL");
    public static string TimeoutQueueUrl => Environment.GetEnvironmentVariable("TIMEOUT_QUEUE_URL");
    public static string BoardGeneratorQueueUrl => Environment.GetEnvironmentVariable("BOARD_GENERATOR_QUEUE_URL");
    public static string BoardBucketName = Environment.GetEnvironmentVariable("BOARD_BUCKET_NAME");
    
    public static string GameDynamoTableName => Environment.GetEnvironmentVariable("GAME_TABLE");
    public static string DictionaryDynamoTableName => Environment.GetEnvironmentVariable("DICTIONARY_TABLE");
    public static string EventBridgeName => Environment.GetEnvironmentVariable("EVENTBRIDGE_NAME");    
}