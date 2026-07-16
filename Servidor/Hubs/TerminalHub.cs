// Servidor/Hubs/TerminalHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using k8s;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using System.Text;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace Servidor.Hubs
{
    [Authorize]
    public class TerminalHub : Hub
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IHubContext<TerminalHub> hubContext;
        private static readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        public TerminalHub(IKubernetesUserClientConnection clientConnection, IHubContext<TerminalHub> hubContext)
        {
            this.clientConnection = clientConnection;
            this.hubContext = hubContext;
        }

        public async Task StartTerminal(string clusterId, string ns, string podName, string container)
        {
            var connectionId = Context.ConnectionId;
            var userIdClaim = Context.User?.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out int userId)) return;

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                if (string.IsNullOrEmpty(container))
                {
                    var pod = await client.CoreV1.ReadNamespacedPodAsync(podName, ns);
                    container = pod.Spec.Containers.First().Name;
                }

                var webSocket = await client.WebSocketNamespacedPodExecAsync(
                    podName, ns, new[] { "sh" }, container,
                    stdin: true, stdout: true, stderr: true, tty: true);

                _sockets[connectionId] = webSocket;

                _ = Task.Run(async () =>
                {
                    var buffer = new byte[8192];
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close) break;

                            if (result.Count > 1)
                            {
                                var channel = buffer[0];
                                var text = Encoding.UTF8.GetString(buffer, 1, result.Count - 1);

                                await hubContext.Clients.Client(connectionId).SendAsync("OutputReceived", text);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _sockets.TryRemove(connectionId, out _);
                        await hubContext.Clients.Client(connectionId).SendAsync("OutputReceived", "\r\n\x1b[31m[Sesión finalizada]\x1b[0m\r\n");
                    }
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("OutputReceived", $"\r\n\x1b[31mError K8s: {ex.Message}\x1b[0m\r\n");
            }
        }

        public async Task SendCommand(string command)
        {
            if (_sockets.TryGetValue(Context.ConnectionId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(command);
                var sendBuffer = new byte[bytes.Length + 1];
                sendBuffer[0] = 0; // Canal 0
                Array.Copy(bytes, 0, sendBuffer, 1, bytes.Length);

                await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_sockets.TryRemove(Context.ConnectionId, out var webSocket))
            {
                if (webSocket.State == WebSocketState.Open)
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
                webSocket.Dispose();
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
