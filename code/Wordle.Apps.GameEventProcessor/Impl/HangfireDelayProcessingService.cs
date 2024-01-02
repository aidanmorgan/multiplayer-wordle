using Hangfire;
using Hangfire.Redis.StackExchange;
using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wordle.Apps.Common;
using Wordle.Clock;
using Wordle.Commands;


namespace Wordle.Apps.GameEventProcessor.Impl;


public class HangfireDelayProcessingService : IDelayProcessingService
{
    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly ILogger<HangfireDelayProcessingService> _logger;
    
    public HangfireDelayProcessingService(IMediator mediator, IClock clock, ILogger<HangfireDelayProcessingService> logger)
    {
        _mediator = mediator;
        _clock = clock;
        _logger = logger;
    }

    public Task ScheduleRoundUpdate(Guid sessionId, Guid roundId, DateTimeOffset executionTime, CancellationToken token)
    {
        var args = new TimeoutPayload(sessionId, roundId);

        var delaySeconds = (int)Math.Max(Math.Ceiling(executionTime.Subtract(_clock.UtcNow()).TotalSeconds), 0);

        if (delaySeconds > 0)
        {
            var id = BackgroundJob.Schedule<IDelayProcessingService>(
                x => x.HandleTimeout(args),
                TimeSpan.FromSeconds(delaySeconds));
            
            _logger.LogInformation("Job {Id} scheduled to check Session {SessionId} in {DelaySeconds} seconds (at: {JobTime})", id, sessionId, delaySeconds, executionTime);
        }
        else
        {
            var id = BackgroundJob.Enqueue<IDelayProcessingService>(
                x => x.HandleTimeout(args));
            
            _logger.LogInformation("Job {Id} scheduled to check Session {SessionId} enqueued", id, sessionId);
        }

        
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken token)
    {
        return Task.Run(async () =>
        {
            using var jobServer = new BackgroundJobServer(new BackgroundJobServerOptions()
                {
                    ServerName = $"{EnvironmentVariables.InstanceType}#{EnvironmentVariables.InstanceId}",
                    SchedulePollingInterval = TimeSpan.FromSeconds(1)
                }, new RedisStorage(
                    await ConnectionMultiplexer.ConnectAsync(EnvironmentVariables.RedisServer), 
                    new RedisStorageOptions() { }
                )
            );
            
            await jobServer.WaitForShutdownAsync(token);
        });
    }

    public async Task HandleTimeout(TimeoutPayload payload)
    {
        try
        {
            await _mediator.Send(new EndActiveRoundCommand(payload.SessionId, payload.RoundId));
        }
        catch (CommandException x)
        {
            _logger.LogError(x, "Attempt to end Round {RoundId} for Session {SessionId} failed", payload.RoundId, payload.SessionId);
        }
    }
}