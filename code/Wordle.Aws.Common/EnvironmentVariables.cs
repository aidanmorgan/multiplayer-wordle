namespace Wordle.Aws.Common;

public  static class EnvironmentVariables
{
    public static string EventQueueUrl = Environment.GetEnvironmentVariable("EVENT_BUS_QUEUE_URL") ?? "development_wordle_events_queue";
    public static string TimeoutQueueUrl = Environment.GetEnvironmentVariable("TIMEOUT_QUEUE_URL") ?? "development_wordle_timeout_queue";
    
    public static string GameDynamoTableName = Environment.GetEnvironmentVariable("") ?? "development_wordle_game";
    public static string DictionaryDynamoTableName = Environment.GetEnvironmentVariable("") ?? "wordle_dictionary";
    public static string EventBridgeName = Environment.GetEnvironmentVariable("") ?? "development-wordle-events";
}