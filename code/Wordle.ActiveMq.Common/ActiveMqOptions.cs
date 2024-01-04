namespace Wordle.ActiveMq.Common;

public abstract class ActiveMqOptions
{
    public static readonly Func<Type, string> TopicNamer;

    static ActiveMqOptions()
    {
        TopicNamer = (x) => $"wordle.{x.Name}".Replace(".", "");
    }
}