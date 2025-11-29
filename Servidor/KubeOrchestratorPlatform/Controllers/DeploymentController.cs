using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Deployment;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/deployments")]
    public class DeploymentsController : ControllerBase
    {
        private readonly IDeploymentService _service;
        private readonly ICurrentUserService _currentUser;

        public DeploymentsController(IDeploymentService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Obtiene una lista resumen de los deployments en un namespace.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(string clusterId, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = _currentUser.UserId;
                var deployments = await _service.GetAllAsync(userId, clusterId, @namespace);
                
                // Proyección: Devolvemos solo lo necesario para una tabla/lista
                var result = deployments.Select(d => new 
                {
                    Name = d.Metadata.Name,
                    Namespace = d.Metadata.NamespaceProperty,
                    // Muestra "2/3" réplicas
                    Replicas = $"{d.Status.AvailableReplicas ?? 0}/{d.Spec.Replicas}", 
                    // Obtiene la primera imagen como referencia
                    Image = d.Spec.Template.Spec.Containers.FirstOrDefault()?.Image,
                    Age = (DateTime.UtcNow - d.Metadata.CreationTimestamp?.ToUniversalTime())?.TotalDays >= 1 
                          ? $"{(int?)(DateTime.UtcNow - d.Metadata.CreationTimestamp?.ToUniversalTime())?.TotalDays}d"
                          : "New",
                    Status = (d.Status.AvailableReplicas == d.Spec.Replicas) ? "Running" : "Pending"
                });

                return Ok(result);
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { Error = ex.Message }); 
            }
        }

        /// <summary>
        /// Obtiene el detalle completo (JSON crudo de K8s) de un deployment.
        /// Útil para el botón "Ver YAML" o "Detalles".
        /// </summary>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = _currentUser.UserId;
                var deployment = await _service.GetByNameAsync(userId, clusterId, @namespace, name);

                if (deployment == null)
                {
                    return NotFound(new { Message = $"El deployment '{name}' no fue encontrado en el namespace '{@namespace}'." });
                }

                return Ok(deployment);
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { Error = ex.Message }); 
            }
        }

        /// <summary>
        /// Crea un nuevo Deployment con configuración avanzada (Límites, Probes, etc.)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, [FromBody] CreateDeploymentDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = _currentUser.UserId;
                
                await _service.CreateAsync(userId, clusterId, dto);
                
                return StatusCode(201, new { Message = $"Deployment '{dto.Name}' creado e iniciado exitosamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new { Message = $"Ya existe un deployment llamado '{dto.Name}' en el namespace '{dto.Namespace}'." });
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { Error = ex.Message }); 
            }
        }

        /// <summary>
        /// Elimina un deployment.
        /// </summary>
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = _currentUser.UserId;
                
                await _service.DeleteAsync(userId, clusterId, @namespace, name);
                
                return Ok(new { Message = $"Deployment '{name}' eliminado correctamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { Message = $"El deployment '{name}' no existe, no se pudo eliminar." });
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { Error = ex.Message }); 
            }
        }
    }
}