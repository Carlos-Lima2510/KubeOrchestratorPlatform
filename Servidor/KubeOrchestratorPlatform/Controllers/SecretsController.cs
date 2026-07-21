using k8s.Autorest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Core;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Core;
using Servidor.KubeOrchestratorPlatform.Services.Errors;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/clusters/{clusterId}/namespaces/{namespaceName}/secrets")]
    public class SecretsController : ControllerBase
    {
        private readonly ISecretService secretService;
        private readonly ICurrentUserService currentUserService;

        public SecretsController(ISecretService secretService, ICurrentUserService currentUserService)
        {
            this.secretService = secretService;
            this.currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            string clusterId,
            string namespaceName,
            [FromQuery] int limit = 10,
            [FromQuery] string? continueToken = null)
        {
            var userId = currentUserService.UserId;
            var result = await secretService.GetPagedAsync(userId, clusterId, namespaceName, limit, continueToken);
            return Ok(result);
        }

        [HttpGet("names")]
        public async Task<IActionResult> GetNames(string clusterId, string namespaceName)
        {
            var userId = currentUserService.UserId;
            var result = await secretService.GetNamesAsync(userId, clusterId, namespaceName);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, string namespaceName, [FromBody] CreateSecretDTO dto)
        {
            var userId = currentUserService.UserId;
            dto.Namespace = namespaceName;

            try
            {
                await secretService.CreateAsync(userId, clusterId, dto);
                return Ok(new { Message = "Secreto creado exitosamente" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (HttpOperationException ex)
            {
                return StatusCode((int)ex.Response.StatusCode, new { Message = KubernetesErrorHelper.GetFriendlyMessage(ex) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor", Details = KubernetesErrorHelper.GetFriendlyMessage(ex) });
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string clusterId, string namespaceName, string name)
        {
            var userId = currentUserService.UserId;
            await secretService.DeleteAsync(userId, clusterId, namespaceName, name);
            return NoContent();
        }
    }
}