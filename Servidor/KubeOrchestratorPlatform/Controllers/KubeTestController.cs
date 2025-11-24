using k8s;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Services.Parser;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Store;
using Servidor.KubeOrchestratorPlatform.Services.Audit;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KubeTestController : ControllerBase
    {
        private readonly IKubeConfigParser _parser;
        private readonly IUserClusterStore _store;
        private readonly IKubernetesUserClientConnection _clientConnection;
        private readonly IAuditService _audit;

        public KubeTestController(
            IKubeConfigParser parser,
            IUserClusterStore store,
            IKubernetesUserClientConnection clientConnection,
            IAuditService audit)
        {
            _parser = parser;
            _store = store;
            _clientConnection = clientConnection;
            _audit = audit;
        }

        [HttpPost("upload-kubeconfig/{userId}/{clusterId}")]
        public IActionResult UploadKubeConfig(string userId, string clusterId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _audit.LogOperation(userId, $"UploadKubeConfig-{clusterId}", false, "Archivo kubeconfig vacío");
                return BadRequest("Archivo kubeconfig vacío.");
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var yaml = reader.ReadToEnd();
            var parsed = _parser.Parse(yaml);

            var creds = new UserClusterCredentials
            {
                Server = parsed.Server,
                CertificateAuthorityData = parsed.CertificateAuthorityData,
                Token = parsed.Token,
                ClientCertificateData = parsed.ClientCertificateData,
                ClientKeyData = parsed.ClientKeyData,
                AuthType = parsed.AuthType
            };

            try
            {
                _store.Save(int.Parse(userId), clusterId, creds);
                _audit.LogOperation(userId, "UploadKubeConfig", true);
                return Ok(new { message = "Kubeconfig cargado correctamente" });
            }
            catch (KeyNotFoundException ex)
            {
                _audit.LogOperation(userId, "UploadKubeConfig", false, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _audit.LogOperation(userId, "UploadKubeConfig", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

        }

        [HttpDelete("config/{userId}/{clusterId}")]
        public IActionResult DeleteConfig(string userId, string clusterId)
        {
            if (!_store.Exists(int.Parse(userId), clusterId))
            {
                _audit.LogOperation(userId, $"DeleteConfig-{clusterId}", false, "Usuario o cluster no encontrado");
                return NotFound(new { message = "Usuario o cluster no encontrado" });
            }

            _store.Remove(int.Parse(userId), clusterId);
            _audit.LogOperation(userId, $"DeleteConfig-{clusterId}", true);

            return Ok(new { message = "Configuración eliminada correctamente" });
        }

        [HttpGet("pods/{userId}/{clusterId}")]
        public async Task<IActionResult> GetPods(string userId, string clusterId, string ns = "default")
        {
            if (!_store.Exists(int.Parse(userId), clusterId))
            {
                _audit.LogOperation(userId, $"GetPods-{clusterId}", false, "Usuario o cluster no encontrado");
                return NotFound(new { message = "Usuario o cluster no encontrado" });
            }

            try
            {
                var client = _clientConnection.GetClient(int.Parse(userId), clusterId);
                var pods = await client.CoreV1.ListNamespacedPodAsync(ns);
                var podNames = pods.Items.Select(p => p.Metadata.Name);
                _audit.LogOperation(userId, $"GetPods-{clusterId}", true);
                return Ok(podNames);
            }
            catch (Exception ex)
            {
                _audit.LogOperation(userId, $"GetPods-{clusterId}", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("namespaces/{userId}/{clusterId}")]
        public async Task<IActionResult> GetNamespaces(string userId, string clusterId)
        {
            if (!_store.Exists(int.Parse(userId), clusterId))
            {
                _audit.LogOperation(userId, $"GetNamespaces-{clusterId}", false, "Usuario o cluster no encontrado");
                return NotFound(new { message = "Usuario o cluster no encontrado" });
            }

            try
            {
                var client = _clientConnection.GetClient(int.Parse(userId), clusterId);
                var namespaces = await client.CoreV1.ListNamespaceAsync();
                var nsNames = namespaces.Items.Select(n => n.Metadata.Name);
                _audit.LogOperation(userId, $"GetNamespaces-{clusterId}", true);
                return Ok(nsNames);
            }
            catch (Exception ex)
            {
                _audit.LogOperation(userId, $"GetNamespaces-{clusterId}", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("services/{userId}/{clusterId}/{ns}")]
        public async Task<IActionResult> GetServices(string userId, string clusterId, string ns)
        {
            if (!_store.Exists(int.Parse(userId), clusterId))
            {
                _audit.LogOperation(userId, $"GetServices-{clusterId}", false, "Usuario o cluster no encontrado");
                return NotFound(new { message = "Usuario o cluster no encontrado" });
            }

            try
            {
                var client = _clientConnection.GetClient(int.Parse(userId), clusterId);
                var services = await client.CoreV1.ListNamespacedServiceAsync(ns);
                var servicesNames = services.Items.Select(s => s.Metadata.Name);
                _audit.LogOperation(userId, $"GetServices-{clusterId}", true);
                return Ok(servicesNames);
            }
            catch (Exception ex)
            {
                _audit.LogOperation(userId, $"GetServices-{clusterId}", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
