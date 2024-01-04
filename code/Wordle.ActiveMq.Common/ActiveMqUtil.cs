using Apache.NMS;
using Microsoft.Extensions.Logging;

namespace Wordle.ActiveMq.Common;

public static class ActiveMqUtil
{
    public static async Task CloseQuietly(IConnection? conn, ILogger logger)
    {
        if (conn == null)
        {
            return;
        }

        try
        {
            await conn.CloseAsync();
        }
        catch (Exception x)
        {
            logger.LogDebug(x, $"Suppressed Exception closing ActiveMq Connection.");
        }
    }
    
    public static async Task CloseQuietly(ISession? conn, ILogger logger)
    {
        if (conn == null)
        {
            return;
        }
        
        try
        {
            await conn.CloseAsync();
        }
        catch (Exception x)
        {
            logger.LogDebug(x, $"Suppressed Exception closing ActiveMq Session.");
        }
    }

    public static async Task CloseQuietly(IMessageProducer? conn, ILogger logger)
    {
        if (conn == null)
        {
            return;
        }
        
        try
        {
            await conn.CloseAsync();
        }
        catch (Exception x)
        {
            logger.LogDebug(x, $"Suppressed Exception closing ActiveMq Producer.");
        }
    }
    
    public static async Task CloseQuietly(IMessageConsumer? conn, ILogger logger)
    {
        if (conn == null)
        {
            return;
        }
        
        try
        {
            await conn.CloseAsync();
        }
        catch (Exception x)
        {
            logger.LogDebug(x, $"Suppressed Exception closing ActiveMq Consumer.");
        }
    }


}