using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Cluster.Configuration;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using k8s;
using Servidor.KubeOrchestratorPlatform.Repositories;
using Servidor.KubeOrchestratorPlatform.Services.Metrics;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters")]
    public class ClustersController : ControllerBase
    {
        private readonly IClusterConfigurationService configurationService;
        private readonly IMetricsService metricsService;
        private readonly ICurrentUserService currentUserService;
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IUserClusterRepository userClusterRepository;
        public ClustersController(
            IClusterConfigurationService configurationService,
            ICurrentUserService currentUserService,
            IKubernetesUserClientConnection clientConnection,
            IUserClusterRepository userClusterRepository,
            IMetricsService metricsService
            )
        {
            this.configurationService = configurationService;
            this.currentUserService = currentUserService;
            this.clientConnection = clientConnection;
            this.userClusterRepository = userClusterRepository;
            this.metricsService = metricsService;
        }

        [HttpGet("{clusterId}/status")]
        public async Task<IActionResult> GetClusterStatus(string clusterId)
        {
            var userId = currentUserService.UserId;

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                var version = await client.Version.GetCodeAsync(cancellationToken: cts.Token);

                return Ok(new
                {
                    Status = "Online",
                    Version = version.GitVersion,
                    Message = "Conexión exitosa"
                });
            }
            catch (Exception ex)
            {
                string reason = "Offline";
                string detail = ex.Message;

                if (ex is TaskCanceledException)
                {
                    detail = "El clúster no responde (Timeout). Probablemente esté apagado o bloqueado por firewall.";
                }
                else if (ex is k8s.Autorest.HttpOperationException httpEx)
                {
                    if (httpEx.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        detail = "Credenciales inválidas o expiradas.";
                    else
                        detail = $"Error HTTP {httpEx.Response.StatusCode}";
                }

                return Ok(new
                {
                    Status = reason,
                    Message = detail
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClusters()
        {
            try
            {
                var userId = currentUserService.UserId;
                var clusters = await userClusterRepository.GetAllClustersAsync(userId);

                var result = clusters.Select(c => new
                {
                    Id = c.PublicClusterId,
                    Name = c.DisplayName,
                    Server = c.ServerUrl,
                    IsActive = c.IsActive
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("{clusterId}/config")]
        public async Task<IActionResult> UploadKubeConfig(string clusterId, [FromForm] KubeConfigDTO kubeConfig)
        {
            if (kubeConfig.File == null || kubeConfig.File.Length == 0)
                return BadRequest("El archivo kubeconfig es obligatorio.");

            try
            {
                var userId = currentUserService.UserId;

                using var stream = kubeConfig.File.OpenReadStream();

                var result = await configurationService.UploadKubeConfigAsync(userId, clusterId, stream);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error procesando configuración.", details = ex.Message });
            }
        }

        [HttpDelete("{clusterId}")]
        public async Task<IActionResult> DeleteConfig(string clusterId)
        {
            try
            {
                var userId = currentUserService.UserId;
                await configurationService.DeleteConfigurationAsync(userId, clusterId);

                return Ok(new { message = "Configuración eliminada correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{clusterId}/metrics")]
        public async Task<IActionResult> GetMetrics(string clusterId)
        {
            try
            {
                var userId = currentUserService.UserId;
                var metrics = await metricsService.GetClusterMetricsAsync(userId, clusterId);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "No se pudieron obtener las métricas. Asegúrese de tener 'metrics-server' instalado en el cluster.", details = ex.Message });
            }
        }
    }
}