using MediatR;
using Wordle.Events;

namespace Wordle.Common;

public interface IAllEventHandlers :
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