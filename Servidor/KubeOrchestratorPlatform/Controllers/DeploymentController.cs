using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Deployment;
using Servidor.KubeOrchestratorPlatform.Services.Errors;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/clusters/{clusterId}/deployments")]
    [Authorize]
    public class DeploymentsController : ControllerBase
    {
        private readonly IDeploymentService deploymentService;
        private readonly ICurrentUserService currentUserService;

        public DeploymentsController(IDeploymentService service, ICurrentUserService currentUser)
        {
            deploymentService = service;
            currentUserService = currentUser;
        }

        /// <summary>
        /// Obtiene una lista resumen de los deployments en un namespace.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(string clusterId, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                var deployments = await deploymentService.GetAllAsync(userId, clusterId, @namespace);
                
                var result = deployments.Select(d => new 
                {
                    Name = d.Metadata.Name,
                    Namespace = d.Metadata.NamespaceProperty,
                    Replicas = $"{d.Status.AvailableReplicas ?? 0}/{d.Spec.Replicas}", 
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
                return StatusCode(500, new { Message = KubernetesErrorHelper.GetFriendlyMessage(ex) });
            }
        }

        /// <summary>
        /// Obtiene el detalle completo (JSON crudo de K8s) de un deployment.
        /// </summary>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                var deployment = await deploymentService.GetByNameAsync(userId, clusterId, @namespace, name);

                if (deployment == null)
                {
                    return NotFound(new { Message = $"El deployment '{name}' no fue encontrado en el namespace '{@namespace}'." });
                }

                return Ok(deployment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = KubernetesErrorHelper.GetFriendlyMessage(ex) });
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
                var userId = currentUserService.UserId;
                
                await deploymentService.CreateAsync(userId, clusterId, dto);
                
                return StatusCode(201, new { Message = $"Deployment '{dto.Name}' creado e iniciado exitosamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new { Message = $"Ya existe un deployment llamado '{dto.Name}' en el namespace '{dto.Namespace}'." });
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                return StatusCode((int)ex.Response.StatusCode, new { Message = KubernetesErrorHelper.GetFriendlyMessage(ex) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = KubernetesErrorHelper.GetFriendlyMessage(ex) });
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
                var userId = currentUserService.UserId;
                
                await deploymentService.DeleteAsync(userId, clusterId, @namespace, name);
                
                return Ok(new { Message = $"Deployment '{name}' eliminado correctamente." });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { Message = $"El deployment '{name}' no existe, no se pudo eliminar." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = KubernetesErrorHelper.GetFriendlyMessage(ex) });
            }
        }
    }
}