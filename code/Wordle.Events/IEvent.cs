using MediatR;

namespace Wordle.Events;

public interface IEvent: INotification
{
    Guid Id { get; }
    
    string EventType { get; set; }
}