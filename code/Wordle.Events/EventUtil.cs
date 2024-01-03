using MediatR;

namespace Wordle.Events;

public class EventUtil
{
    public static List<Type> GetAllEventTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IEvent).IsAssignableFrom(p) && !p.IsAbstract)
            .ToList();

    }
}