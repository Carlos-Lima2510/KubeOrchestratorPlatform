using k8s;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Metrics;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Servidor.Hubs
{
    [Authorize] 
    public class ClusterHub : Hub
    {
        private readonly IMetricsService metricsService;
        private readonly IKubernetesUserClientConnection clientConnection; 
        private readonly ILogger<ClusterHub> logger;

        public ClusterHub(
            IMetricsService metricsService, 
            ILogger<ClusterHub> logger, 
            IKubernetesUserClientConnection clientConnection)
        {
            this.metricsService = metricsService;
            this.logger = logger;
            this.clientConnection = clientConnection;
        }
        public async IAsyncEnumerable<object> StreamClusterMetrics(
            string clusterId, 
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var userIdClaim = Context.User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new HubException("Usuario no identificado");
            }

            logger.LogInformation("Iniciando stream de métricas para Usuario {UserId} en Cluster {ClusterId}", userId, clusterId);

            while (!cancellationToken.IsCancellationRequested)
            {
                object? result = null;
                try 
                {
                    var metrics = await metricsService.GetClusterMetricsAsync(userId, clusterId);
                    result = metrics.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    result = new { error = "Error obteniendo métricas", details = ex.Message };
                }

                if (result != null)
                {
                    yield return result;
                }

                try {
                    await Task.Delay(5000, cancellationToken);
                } catch (TaskCanceledException) {
                    break; 
                }
            }
            
            logger.LogInformation("Stream finalizado para Usuario {UserId}", userId);
        }

        public async IAsyncEnumerable<object> StreamClusterStatus(
            string clusterId, 
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var userIdClaim = Context.User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new HubException("Usuario no identificado");

            logger.LogInformation("Iniciando monitor de estado para Cluster {ClusterId}", clusterId);

            while (!cancellationToken.IsCancellationRequested)
            {
                object result;
                try
                {
                    var client = clientConnection.GetClient(userId, clusterId);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    
                    var version = await client.Version.GetCodeAsync(cancellationToken: cts.Token);

                    result = new { Status = "Online", Version = version.GitVersion, Message = "Conectado" };
                }
                catch (Exception ex)
                {
                    string msg = ex is TaskCanceledException ? "Tiempo de espera agotado" : ex.Message;
                    result = new { Status = "Offline", Version = "-", Message = msg };
                }

                yield return result;

                try { await Task.Delay(30000, cancellationToken); } 
                catch { break; }
            }
        }
    }
}