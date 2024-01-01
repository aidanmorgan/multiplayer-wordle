using MediatR;
using Wordle.Events;

namespace Wordle.Aws.Common;

public interface IEventPublisher :
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