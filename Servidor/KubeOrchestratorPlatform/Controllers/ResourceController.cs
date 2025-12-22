using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Resources;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/resources")]
    [Authorize]
    public class ResourcesController : ControllerBase
    {
        private readonly IResourceService resourceService;

        public ResourcesController(IResourceService resourceService)
        {
            this.resourceService = resourceService;
        }

        [HttpGet("{type}/{name}/yaml")]
        public async Task<IActionResult> GetYaml(
            string clusterId,
            string type,
            string name,
            [FromQuery] string @namespace)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("id")?.Value!);

                var yaml = await resourceService.GetResourceYamlAsync(userId, clusterId, @namespace, type, name);

                return Ok(yaml);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al obtener YAML: {ex.Message}");
            }
        }
    }
}