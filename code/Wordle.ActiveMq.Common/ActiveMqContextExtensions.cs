using Microsoft.Extensions.Logging;
using Polly;

namespace Wordle.ActiveMq.Common;

public static class ActiveMqContextExtensions
{
    private const string LoggerKey = $"{nameof(ActiveMqContextExtensions)}.Logger";

    public static void SetLogger(this Context ctx, ILogger logger)
    {
        ctx[LoggerKey] = logger;
    }

    public static Context Initialise(this Context ctx, ILogger logger)
    {
        SetLogger(ctx, logger);

        return ctx;
    }

    public static ILogger GetLogger(this Context ctx)
    {
        return ctx[LoggerKey] as ILogger;
    }
}