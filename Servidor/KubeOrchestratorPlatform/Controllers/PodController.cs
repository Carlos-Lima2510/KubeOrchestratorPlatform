using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Pods;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/pods")]
    public class PodsController : ControllerBase
    {
        private readonly IPodService podService;
        private readonly ICurrentUserService currentUserService;

        public PodsController(IPodService podService, ICurrentUserService currentUserService)
        {
            this.podService = podService;
            this.currentUserService = currentUserService;
        }

        [HttpGet("by-deployment/{deploymentName}")]
        public async Task<IActionResult> GetByDeployment(string clusterId, string deploymentName, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                var pods = await podService.GetByDeploymentAsync(userId, clusterId, @namespace, deploymentName);
                return Ok(pods);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{podName}/logs")]
        public async Task<IActionResult> GetLogs(string clusterId, string podName, [FromQuery] string @namespace, [FromQuery] string container)
        {
            try
            {
                var userId = currentUserService.UserId;
                var stream = await podService.GetLogsAsync(userId, clusterId, @namespace, podName, container);
                return File(stream, "text/plain");
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                var errorBody = ex.Response.Content;

                if (errorBody.Contains("x509: certificate is valid for") && errorBody.Contains("not"))
                {
                    return StatusCode(500, "ALERTA DE INFRAESTRUCTURA: La IP de tu nodo Kubernetes ha cambiado y los certificados internos no coinciden.");
                }

                if (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound("El pod o contenedor ya no existe. Probablemente se reinició.");
                }

                if (ex.Response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return BadRequest($"Error en la petición: {ex.Message}. Verifica el nombre del contenedor.");
                }

                return StatusCode((int)ex.Response.StatusCode, $"Error de Kubernetes: {errorBody}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}