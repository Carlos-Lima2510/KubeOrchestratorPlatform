using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Ingress;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network.Ingress;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/clusters/{clusterId}/ingresses")]
    public class IngressController : ControllerBase
    {
        private readonly IIngressService ingressService;
        private readonly ICurrentUserService currentUserService;

        public IngressController(IIngressService ingressService, ICurrentUserService currentUserService)
        {
            this.ingressService = ingressService;
            this.currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string clusterId, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                var ingresses = await ingressService.GetAllAsync(userId, clusterId, @namespace);

                var result = ingresses.Select(i => new
                {
                    Name = i.Metadata.Name,
                    Namespace = i.Metadata.NamespaceProperty,
                    Hosts = i.Spec.Rules.Select(r => r.Host),
                    LoadBalancerIP = i.Status?.LoadBalancer?.Ingress?.FirstOrDefault()?.Ip ?? "-",
                    Age = (DateTime.UtcNow - i.Metadata.CreationTimestamp?.ToUniversalTime())?.TotalDays >= 1
                          ? $"{(int?)(DateTime.UtcNow - i.Metadata.CreationTimestamp?.ToUniversalTime())?.TotalDays}d"
                          : "New"
                });

                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create(string clusterId, [FromBody] CreateIngressDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var userId = currentUserService.UserId;
                var result = await ingressService.CreateAsync(userId, clusterId, dto);
                return StatusCode(201, result);
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                return StatusCode((int)ex.Response.StatusCode, new { Error = ex.Response.Content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string clusterId, string name, [FromQuery] string @namespace = "default")
        {
            try
            {
                var userId = currentUserService.UserId;
                await ingressService.DeleteAsync(userId, clusterId, @namespace, name);
                return Ok(new { Message = "Ingress eliminado." });
            }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }
    }
}
