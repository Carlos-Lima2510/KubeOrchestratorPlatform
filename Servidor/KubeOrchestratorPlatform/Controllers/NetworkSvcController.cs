using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Network;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/services")]
    [Authorize]
    public class ServicesController : ControllerBase
    {
        private readonly INetworkService networkService;
        private readonly ICurrentUserService currentUserService;

        public ServicesController(INetworkService service, ICurrentUserService currentUser)
        {
            networkService = service;
            currentUserService = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string clusterId, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                var services = await networkService.GetAllAsync(userId, clusterId, @namespace);

                var result = services.Select(s => new
                {
                    Name = s.Metadata.Name,
                    Namespace = s.Metadata.NamespaceProperty,
                    Type = s.Spec.Type,
                    ClusterIP = s.Spec.ClusterIP,
                    Ports = s.Spec.Ports?.Select(p => $"{p.Port}:{p.NodePort}/{p.Protocol}") ?? Enumerable.Empty<string>(),
                    ExternalIP = s.Status.LoadBalancer?.Ingress?.FirstOrDefault()?.Ip ?? "-",
                    Age = (DateTime.UtcNow - s.Metadata.CreationTimestamp?.ToUniversalTime())?.TotalDays >= 1
                          ? $"{(int?)(DateTime.UtcNow - s.Metadata.CreationTimestamp?.ToUniversalTime())?.TotalDays}d"
                          : "New"
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                var service = await networkService.GetByNameAsync(userId, clusterId, @namespace, name);

                if (service == null)
                {
                    return NotFound(new { Message = $"El servicio '{name}' no fue encontrado en el namespace '{@namespace}'." });
                }

                return Ok(service);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, [FromBody] CreateServiceDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = currentUserService.UserId;

                var result = await networkService.CreateAsync(userId, clusterId, dto);

                return StatusCode(201, result);
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new { Message = $"Ya existe un servicio llamado '{dto.Name}' en el namespace '{dto.Namespace}'." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;

                await networkService.DeleteAsync(userId, clusterId, @namespace, name);

                return Ok(new { Message = $"Servicio '{name}' eliminado correctamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { Message = $"El servicio '{name}' no existe." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}