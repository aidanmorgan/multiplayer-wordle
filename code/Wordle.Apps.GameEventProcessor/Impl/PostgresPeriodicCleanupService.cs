using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Logging;
using Wordle.Clock;
using Wordle.EfCore;
using Wordle.Model;

namespace Wordle.Apps.GameEventProcessor.Impl;

public class PostgresPeriodicCleanupService : IPeriodicCleanupService
{
    // this needs to be kept bigger than any of the largest maximum session times that can be possible
    
    private readonly GameEventProcessorOptions _options;
    private readonly IClock _clock;
    private readonly WordleEfCoreSettings _persistence;
    private readonly ILogger<PostgresPeriodicCleanupService> _logger;

    public PostgresPeriodicCleanupService(GameEventProcessorOptions options, IClock clock, WordleEfCoreSettings persistence, ILogger<PostgresPeriodicCleanupService> logger)
    {
        _options = options;
        _clock = clock;
        _persistence = persistence;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var task = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.PreiodicCleanupFrequency, token);

                    var rowCount = await _persistence.Connection.ExecuteAsync(
                        @"UPDATE sessions
                         SET state = @failedStateId, activeroundid = NULL, activeroundend = NULL
                         WHERE state = @activeStateId AND activeroundend < @time",
                        new
                        {
                            FailedStateId = SessionState.FAIL,
                            ActiveStateId = SessionState.ACTIVE,
                            Time = _clock.UtcNow().Subtract(_options.MaximumSessionAge)
                        }
                    );

                    _logger.LogInformation("{ServiceName} force-expired {Count} Sessions",
                        nameof(PostgresPeriodicCleanupService), rowCount);
                }
                catch (DbException x)
                {
                    _logger.LogCritical(x, $"Exception thrown force-expiring Sessions");
                    // don't care
                }
            }
        });

        await Task.WhenAll(task);

        return;
    }
}