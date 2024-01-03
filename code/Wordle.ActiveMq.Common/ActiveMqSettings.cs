namespace Wordle.ActiveMq.Common;

public abstract class ActiveMqSettings
{
    public static readonly Func<Type, string> TopicNamer;

    static ActiveMqSettings()
    {
        TopicNamer = (x) => $"wordle.{x.Name}";
    }
}