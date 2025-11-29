using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Cluster.Configuration;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters")]
    public class ClustersController : ControllerBase
    {
        private readonly IClusterConfigurationService configurationService;
        private readonly ICurrentUserService currentUserService;

        public ClustersController(
            IClusterConfigurationService configurationService,
            ICurrentUserService currentUserService)
        {
            this.configurationService = configurationService;
            this.currentUserService = currentUserService;
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