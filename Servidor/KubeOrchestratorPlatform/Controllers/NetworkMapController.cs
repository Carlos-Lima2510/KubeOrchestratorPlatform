using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Observability;
using Servidor.KubeOrchestratorPlatform.Services.Auth;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/network-map")]
    public class NetworkMapController : ControllerBase
    {
        private readonly INetworkMapService _networkMapService;
        private readonly ICurrentUserService _currentUserService;

        public NetworkMapController(INetworkMapService networkMapService, ICurrentUserService currentUserService)
        {
            _networkMapService = networkMapService;
            _currentUserService = currentUserService;
        }

        [HttpGet("{namespaceName}")]
        public async Task<IActionResult> GetMap(string clusterId, string namespaceName)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var result = await _networkMapService.GetNetworkMapAsync(userId, clusterId, namespaceName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "No se pudo generar el mapa de red: " + ex.Message });
            }
        }
    }
}
