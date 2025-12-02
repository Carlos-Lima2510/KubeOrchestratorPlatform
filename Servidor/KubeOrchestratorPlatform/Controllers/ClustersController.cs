using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Cluster.Configuration;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using k8s;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters")]
    public class ClustersController : ControllerBase
    {
        private readonly IClusterConfigurationService configurationService;
        private readonly ICurrentUserService currentUserService;
        private readonly IKubernetesUserClientConnection clientConnection;

        public ClustersController(
            IClusterConfigurationService configurationService,
            ICurrentUserService currentUserService,
            IKubernetesUserClientConnection clientConnection
            )
        {
            this.configurationService = configurationService;
            this.currentUserService = currentUserService;
            this.clientConnection = clientConnection;
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
    }
}