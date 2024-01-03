using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Wordle.Api.Realtime;

public class RealTimeController : Controller
{
    [HttpGet("/v1/tenants/{tenantId}")]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task Subscribe( 
        [FromServices]IWebsocketTenantService service, 
        [FromServices] IHostApplicationLifetime applicationLifetime,
        [FromServices] Serilog.ILogger logger,
        [FromRoute]string tenantId)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            CancellationTokenSource cts = new CancellationTokenSource();
            
            logger.Information("Accepted ws:// from {ConnectionRemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
            
            await service.AddClient(tenantId, webSocket, HttpContext.Connection, applicationLifetime.ApplicationStopping);
        }
        else
        {
            throw new Exception($"Only accepts websocket requests.");
        }
    }
}