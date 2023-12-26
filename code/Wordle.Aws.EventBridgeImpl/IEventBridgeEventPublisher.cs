using MediatR;
using Wordle.Events;

namespace Wordle.Aws.EventBridgeImpl;

public interface IEventBridgeEventPublisher :
    INotificationHandler<GuessAdded>,
    INotificationHandler<NewRoundStarted>,
    INotificationHandler<NewSessionStarted>,
    INotificationHandler<RoundEnded>,
    INotificationHandler<RoundExtended>,
    INotificationHandler<SessionEndedWithFailure>,
    INotificationHandler<SessionEndedWithSuccess>,
    INotificationHandler<SessionTerminated>
{
    
}