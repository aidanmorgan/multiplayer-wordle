using System.Reflection;

namespace Wordle.Apps.Common;

public static class EnvironmentVariables
{
    public static readonly List<string> EnvironmentFiles =
    [
        ".sensitive-local.env",
        ".development-local.env"
    ];

    public static readonly string WordleEnvSearchPathsEnvName = "WORDLE_ENV_SEARCH_PATHS";

    static EnvironmentVariables()
    {
        var envFilesSearchPath = Environment.GetEnvironmentVariable(WordleEnvSearchPathsEnvName);

        if (string.IsNullOrEmpty(envFilesSearchPath))
        {
            Console.WriteLine($"No {WordleEnvSearchPathsEnvName} set in environment, will not overload environment variables.");
            return;
        }
        
        var files = envFilesSearchPath
            .Split(",")
            .SelectMany(x => EnvironmentFiles.Select(y => (x, y)))
            .Select(x => Path.Join(x.x, x.y))
            .Where(x =>
            {
                
                return File.Exists(x);
            })
            .ToList();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            
            lines
                .Where(x => !string.IsNullOrEmpty(x) && x.Count(y=>y=='=') > 0 && !x.StartsWith("#"))
                .Select(x =>
                {
                    var index = x.IndexOf("=", StringComparison.Ordinal);

                    var name = x.Substring(0, index);
                    var value = x.Substring(index + 1);
                    var isOverride = false;

                    if (name.ToUpper().EndsWith("_OVERRIDE"))
                    {
                        name = name.Substring(0, name.Length - "_OVERRIDE".Length);
                        isOverride = true;
                    }
                    
                    return new Tuple<string, string, bool>(name, value, isOverride);
                })
                .ToList()
                .ForEach(x =>
                {
                    var existing = Environment.GetEnvironmentVariable(x.Item1);

                    // if the environment variable is already set then we should definitely not
                    // overwrite it unless it's explicitly specified to be, using the files is for convenience
                    // (because I am super lazy)also the ordering of the files is important, we'll use the values from the 
                    // .sensitive.env over the values for .development.env
                    if (string.IsNullOrEmpty(existing) || x.Item3)
                    {
                        Environment.SetEnvironmentVariable(x.Item1, x.Item2);
                        Console.WriteLine($"Overriding env {x.Item1} with {x.Item2} from file {file}.");
                    }
                });
        }
    }
    
    public static string EventQueueUrl => Environment.GetEnvironmentVariable("EVENT_QUEUE_URL");
    public static string TimeoutQueueUrl => Environment.GetEnvironmentVariable("TIMEOUT_QUEUE_URL");
    public static string BoardGeneratorQueueUrl => Environment.GetEnvironmentVariable("BOARD_GENERATOR_QUEUE_URL");
    public static string EventBridgeName => Environment.GetEnvironmentVariable("EVENTBRIDGE_NAME");
    
    public static string BoardBucketName = Environment.GetEnvironmentVariable("BOARD_BUCKET_NAME");
    
    public static string GameDynamoTableName => Environment.GetEnvironmentVariable("GAME_TABLE");
    public static string DictionaryDynamoTableName => Environment.GetEnvironmentVariable("DICTIONARY_TABLE");

    public static string InstanceId => Environment.GetEnvironmentVariable("INSTANCE_ID") ?? Guid.NewGuid().ToString();
    public static string InstanceType => Environment.GetEnvironmentVariable("INSTANCE_TYPE");
    
    public static string KafkaBootstrapServers => Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
    public static string KafkaEventTopic => Environment.GetEnvironmentVariable("KAFKA_EVENT_TOPIC");
    public static string RedisServer => Environment.GetEnvironmentVariable("REDIS_SERVER");
    public static string RedisTopic => Environment.GetEnvironmentVariable("REDIS_TOPIC");
    public static string ImagesDirectory => Environment.GetEnvironmentVariable("IMAGES_BASEDIRECTORY");
    public static string EmptyGridImageUrl => Environment.GetEnvironmentVariable("EMPTY_GRID_IMAGE_URL");
    public static string ActiveMqBrokerUrl => Environment.GetEnvironmentVariable("ACTIVEMQ_BROKER_URL");
    public static string PostgresHost => Environment.GetEnvironmentVariable("POSTGRES_HOST");
    public static string PostgresUser => Environment.GetEnvironmentVariable("POSTGRES_USER");
    public static string PostgresPassword => Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
    public static string PostgresDatabase => Environment.GetEnvironmentVariable("POSTGRES_DB");
    
    public static string PostgresConnectionString => $"Host={PostgresHost};Username={PostgresUser};Password={PostgresPassword};Database={PostgresDatabase}";


    public static void SetDefault(string key, string value)
    {
        var o = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(o))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public static void SetDefaultInstanceConfig(string type, string id)
    {
        SetDefault("INSTANCE_TYPE", type);
        SetDefault("INSTANCE_ID", id);
    }
}