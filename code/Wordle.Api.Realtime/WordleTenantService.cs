using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Wordle.Events;
using Wordle.Model;
using Wordle.Queries;
using ILogger = Wordle.Logger.ILogger;

namespace Wordle.Api.Realtime;

public interface IWordleTenantService : 
    INotificationHandler<GuessAdded>, 
    INotificationHandler<NewRoundStarted>, 
    INotificationHandler<RoundExtended>, 
    INotificationHandler<SessionEndedWithSuccess>,
    INotificationHandler<SessionEndedWithFailure>,
    INotificationHandler<SessionTerminated>
{
    Task AddClient(string tenantId, WebSocket webSocket, ConnectionInfo httpContextConnection, CancellationToken ct);
}

public class WordleTenantService : IWordleTenantService
{
    // TODO : this is actually the dynamo content leaking and we need to do something about that
    public const string TenantPrefix = "tenant#web#";
    private readonly IDictionary<string, IObservable<ArraySegment<byte>>> _tenantObservables = new Dictionary<string, IObservable<ArraySegment<byte>>>();
    private readonly ReaderWriterLock _observablesLock = new();
    private readonly IScheduler _scheduler = new EventLoopScheduler();
    private readonly IGuessDecimator _guessDecimator;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public WordleTenantService(IMediator mediator, IGuessDecimator decimator, ILogger logger)
    {
        _mediator = mediator;
        _guessDecimator = decimator;
        _logger = logger;
    }

    public async Task Handle(GuessAdded notification, CancellationToken ct)
    {
        var observable = GetObservableForTenant(notification.Tenant);

        if (observable == null)
        {
            return;
        }
        
        var guesses = _mediator.Send(new GetGuessesForRoundQuery(notification.RoundId), ct);
        var options =  _mediator.Send(new GetOptionsForTenantQuery("web", notification.Tenant), ct);

        await Task.WhenAll(guesses, options);
        
        var decimated = _guessDecimator.DecimateGuesses(guesses.Result, options.Result ?? new Options());

        var array = decimated
            .GroupBy(x => x.Word)
            .OrderByDescending(x => x.Count())
            .Select(x => new
            {
                Word = x.Key,
                Users = x.ToList().Select(x => x.User).ToArray()
            })
            .ToArray();
        
        ((Subject<ArraySegment<byte>>)observable)?.OnNextJson(new EventPayload()
        {
            GameId = notification.SessionId,
            Event = "update_guesses",
            Value = array
        });
    }
    
    public async Task Handle(NewRoundStarted notification, CancellationToken cancellationToken)
    {
        var observable = GetObservableForTenant(notification.Tenant);
        
        if (observable == null)
        {
            return;
        }

        ((Subject<ArraySegment<byte>>)observable)?.OnNextJson(new EventPayload()
        {
            GameId = notification.SessionId,
            Event = "round_updated",
            Value = new
            {  
                BoardUrl = "",
                RoundEndTime = notification.RoundExpiry
            }
        });

    }

    public async Task Handle(RoundExtended notification, CancellationToken cancellationToken)
    {
        var observable = GetObservableForTenant(notification.Tenant);
        
        if (observable == null)
        {
            return;
        }

        ((Subject<ArraySegment<byte>>)observable)?.OnNextJson(new EventPayload()
        {
            GameId = notification.SessionId,
            Event = "round_extended",
            Value = new
            {  
                RoundEndTime = notification.RoundExpiry
            }
        });
    }

    public async Task Handle(SessionEndedWithSuccess notification, CancellationToken cancellationToken)
    {
        var observable = GetObservableForTenant(notification.Tenant);
        
        if (observable == null)
        {
            return;
        }
        
        ((Subject<ArraySegment<byte>>)observable)?.OnNextJson(new EventPayload()
        {
            GameId = notification.SessionId,
            Event = "game_success",
        });
    }

    public async Task Handle(SessionEndedWithFailure notification, CancellationToken cancellationToken)
    {
        var observable = GetObservableForTenant(notification.Tenant);
        
        if (observable == null)
        {
            return;
        }

        ((Subject<ArraySegment<byte>>)observable)?.OnNextJson(new EventPayload()
        {
            GameId = notification.SessionId,
            Event = "game_failure",
        });
    }

    public async Task Handle(SessionTerminated notification, CancellationToken cancellationToken)
    {
        var observable = GetObservableForTenant(notification.Tenant);
        ((Subject<ArraySegment<byte>>)observable)?.OnNextJson(new EventPayload()
        {
            GameId = notification.SessionId,
            Event = "game_terminated",
        });
    }

    public async Task AddClient(string tenantId, WebSocket webSocket, ConnectionInfo connection, CancellationToken ct)
    {
        if (!tenantId.StartsWith(TenantPrefix))
        {
            
            tenantId = TenantPrefix + tenantId;
        }

        var observable = GetObservableForTenant(tenantId, CreateObserver)!;
        
        var disposable = observable
            .ObserveOn(_scheduler)
            .Select(x => Observable.FromAsync(async () =>
                {
                    try
                    {
                        await webSocket.SendAsync(x, WebSocketMessageType.Text, true, ct);
                    }
                    catch (Exception x)
                    {
                        _logger.Log($"Exception thrown sending to websocket {connection.RemoteIpAddress}");
                    }
                }))
            .Concat()
            .Subscribe();

        using (disposable)
        {
            await WaitForClientDisconnect(webSocket, ct);
        }

        await ForceClientDisconnect(webSocket, ct);
    }

    private async Task ForceClientDisconnect(WebSocket webSocket, CancellationToken ct)
    {
            try 
            {
                if (webSocket.State is WebSocketState.Closed or WebSocketState.Aborted)
                {
                    return;
                }
                else {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or WebSocketException)
            {
            } 

    }

    private async Task WaitForClientDisconnect(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8];
        WebSocketReceiveResult? receiveResult = null;
        do {
            try 
            {
                if (webSocket.State is WebSocketState.Open or WebSocketState.Connecting) {
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or WebSocketException)
            {
                break;
            } 
        } while ((webSocket.State is WebSocketState.Open or WebSocketState.Connecting) && receiveResult != null && !receiveResult.CloseStatus.HasValue);
    }
    
    /// <summary>
    /// Attempts to get the IObservable for the tenant if it is registered, if it is not registered then the optional
    /// factory can be used to create a new one.
    /// </summary>
    private IObservable<ArraySegment<byte>>? GetObservableForTenant(string id, Func<IObservable<ArraySegment<byte>>> factory = null)
    {
        try
        {
            _observablesLock.AcquireReaderLock(TimeSpan.FromSeconds(5));
            if (_tenantObservables.TryGetValue(id, out var tenant))
            {
                return tenant;
            }
        }
        finally
        {
            _observablesLock.ReleaseReaderLock();
        }

        if (factory == null)
        {
            return null;
        }

        try
        {
            _observablesLock.AcquireWriterLock(TimeSpan.FromSeconds(5));

            if (_tenantObservables.TryGetValue(id, out var tenant))
            {
                return tenant;
            }

            var observable = factory();
            _tenantObservables[id] = observable;
            return observable;
        }
        finally
        {
            _observablesLock.ReleaseWriterLock();
        }
    }
    
    private static IObservable<ArraySegment<byte>> CreateObserver()
    {
        return new Subject<ArraySegment<byte>>();
    }
}

public static class SubjectExtensions
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

    static SubjectExtensions()
    {
        SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    }
    
    public static void OnNextJson(this Subject<ArraySegment<byte>> subject, object o)
    {
        var asStr = JsonConvert.SerializeObject(o, SerializerSettings);
        subject.OnNext(new ArraySegment<byte>(Encoding.GetBytes(asStr)));
    }
}

public struct EventPayload
{
    public string Event { get; set; }
    public object Value { get; set; }
    public Guid GameId { get; set; }
}


