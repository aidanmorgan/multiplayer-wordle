using MediatR;

namespace Wordle.Events;

public interface IEvent: INotification
{
    Guid Id { get; }
    
    string EventType { get; set; }
    
    string Tenant { get; set; }
    
    string EventSourceId { get; set; }
    
    string EventSourceType { get; set; }
    
    DateTimeOffset Timestamp { get; set; }
}