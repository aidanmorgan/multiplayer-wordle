namespace Wordle.Kafka.Common;

public class KafkaSettings
{
    public string BootstrapServers { get; init; }
    public string Topic { get; init; }
    
    public string InstanceType { get; init; }
    public string InstanceId { get; init; }
}