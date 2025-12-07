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
    }
}