using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Requests;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Namespace;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/namespaces")]
    public class NamespacesController : ControllerBase
    {
        private readonly INamespaceService namespaceService;
        private readonly ICurrentUserService currentUserService;
        public NamespacesController(INamespaceService namespaceService, ICurrentUserService currentUserService)
        {
            this.namespaceService = namespaceService;
            this.currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string clusterId)
        {
            try
            {
                var userId = currentUserService.UserId;

                var result = await namespaceService.GetAllAsync(userId, clusterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{clusterName}")]
        public async Task<IActionResult> GetByName(string clusterId, string clusterName)
        {
            try
            {
                var userId = currentUserService.UserId;

                var result = await namespaceService.GetByNameAsync(userId, clusterId, clusterName);

                if (result == null)
                {
                    return NotFound(new { Message = $"El namespace '{clusterName}' no existe en el clúster." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, [FromBody] CreateNamespaceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = currentUserService.UserId;

                await namespaceService.CreateAsync(userId, clusterId, request.Name);

                return StatusCode(201, new { Message = $"Namespace '{request.Name}' creado exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{clusterName}")]
        public async Task<IActionResult> Delete(string clusterId, string clusterName)
        {
            try
            {
                var userId = currentUserService.UserId;

                await namespaceService.DeleteAsync(userId, clusterId, clusterName);

                return Ok(new { Message = $"Namespace '{clusterName}' eliminado correctamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { Message = $"El namespace '{clusterName}' no existe." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
