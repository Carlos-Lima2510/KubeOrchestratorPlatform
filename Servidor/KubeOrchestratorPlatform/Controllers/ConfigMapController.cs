using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Core;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Core;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/clusters/{clusterId}/namespaces/{namespaceName}/config-maps")]
    public class ConfigMapsController : ControllerBase
    {
        private readonly IConfigMapService configMapService;
        private readonly ICurrentUserService currentUserService;

        public ConfigMapsController(IConfigMapService configMapService, ICurrentUserService currentUserService)
        {
            this.configMapService = configMapService;
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
            var result = await configMapService.GetPagedAsync(userId, clusterId, namespaceName, limit, continueToken);
            return Ok(result);
        }

        [HttpGet("names")]
        public async Task<IActionResult> GetNames(string clusterId, string namespaceName)
        {
            var userId = currentUserService.UserId;
            var result = await configMapService.GetNamesAsync(userId, clusterId, namespaceName);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, string namespaceName, [FromBody] CreateConfigMapDTO dto)
        {
            var userId = currentUserService.UserId;
            dto.Namespace = namespaceName;

            await configMapService.CreateAsync(userId, clusterId, dto);
            return Ok(new { Message = "ConfigMap creado exitosamente" });
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string clusterId, string namespaceName, string name)
        {
            var userId = currentUserService.UserId;
            await configMapService.DeleteAsync(userId, clusterId, namespaceName, name);
            return NoContent();
        }
    }
}