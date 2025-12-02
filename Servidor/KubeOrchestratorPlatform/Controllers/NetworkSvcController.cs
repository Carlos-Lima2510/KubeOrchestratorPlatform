using Microsoft.AspNetCore.Mvc;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Network;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/services")]
    public class ServicesController : ControllerBase
    {
        private readonly INetworkService _service;
        private readonly ICurrentUserService _currentUser;

        public ServicesController(INetworkService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Obtiene todos los servicios en un namespace.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(string clusterId, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = _currentUser.UserId;
                var services = await _service.GetAllAsync(userId, clusterId, @namespace);

                // Proyección ligera para la tabla del frontend
                var result = services.Select(s => new
                {
                    Name = s.Metadata.Name,
                    Namespace = s.Metadata.NamespaceProperty,
                    Type = s.Spec.Type,
                    ClusterIP = s.Spec.ClusterIP,
                    // Mostramos puertos como "80:30080/TCP"
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
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el detalle de un servicio específico.
        /// </summary>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = _currentUser.UserId;
                var service = await _service.GetByNameAsync(userId, clusterId, @namespace, name);

                if (service == null)
                {
                    return NotFound(new { Message = $"El servicio '{name}' no fue encontrado en el namespace '{@namespace}'." });
                }

                return Ok(service); // Devuelve el objeto V1Service (ya limpiado en el servicio)
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo Service.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, [FromBody] CreateServiceDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = _currentUser.UserId;

                // Capturamos el resultado del servicio
                var result = await _service.CreateAsync(userId, clusterId, dto);

                // Devolvemos 201 Created con el cuerpo del resultado (incluyendo el warning si existe)
                return StatusCode(201, result);
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new { Message = $"Ya existe un servicio llamado '{dto.Name}' en el namespace '{dto.Namespace}'." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un Service.
        /// </summary>
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = _currentUser.UserId;

                await _service.DeleteAsync(userId, clusterId, @namespace, name);

                return Ok(new { Message = $"Servicio '{name}' eliminado correctamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { Message = $"El servicio '{name}' no existe." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}