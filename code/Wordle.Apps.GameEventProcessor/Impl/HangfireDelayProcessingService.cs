using Hangfire;
using Hangfire.Common;
using Hangfire.Redis.StackExchange;
using MediatR;
using StackExchange.Redis;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;
using Wordle.Logger;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Apps.GameEventProcessor.Impl;


public class HangfireDelayProcessingService : AbstractDelayProcessingService
{
    private readonly IClock _clock;
    
    public HangfireDelayProcessingService(IMediator mediator, IClock clock, ILogger logger) : base(mediator, logger)
    {
        _clock = clock;
    }

    public override Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken token)
    {
        var args = new TimeoutPayload(sessionId, roundId);

        var delaySeconds = (int)Math.Max(Math.Ceiling(executionTime.Subtract(_clock.UtcNow()).TotalSeconds), 0);

        if (delaySeconds > 0)
        {
            var id = BackgroundJob.Schedule<HangfireDelayProcessingService>(
                x => x.HandleTimeout(args),
                TimeSpan.FromSeconds(delaySeconds));
            
            Logger.Log($"Job {id} scheduled to check Session {sessionId} in {delaySeconds} seconds.");
        }
        else
        {
            var id = BackgroundJob.Enqueue<HangfireDelayProcessingService>(
                x => x.HandleTimeout(args));
            
            Logger.Log($"Job {id} scheduled to check Session {sessionId} enqueued.");
        }

        
        return Task.CompletedTask;
    }

    public override Task RunAsync(CancellationToken token)
    {
        using var jobServer = new BackgroundJobServer(new BackgroundJobServerOptions()
        {
            ServerName = $"{EnvironmentVariables.InstanceType}#{EnvironmentVariables.InstanceId}",
            SchedulePollingInterval = TimeSpan.FromMilliseconds(50),
        }, new RedisStorage(ConnectionMultiplexer.Connect(EnvironmentVariables.RedisServer), 
            new RedisStorageOptions()
            {
                
            })
        );

        return jobServer.WaitForShutdownAsync(token);
    }
}