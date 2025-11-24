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
        private readonly IUserClusterStore store;
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IAuditService audit;

        public KubeTestController(
            IKubeConfigParser parser,
            IUserClusterStore store,
            IKubernetesUserClientConnection clientConnection,
            IAuditService audit)
        {
            _parser = parser;
            this.store = store;
            this.clientConnection = clientConnection;
            this.audit = audit;
        }

        [HttpPost("upload-kubeconfig/{userId}")]
        public IActionResult UploadKubeConfig(string userId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                audit.LogOperation(userId, "UploadKubeConfig", false, "Archivo kubeconfig vacío");
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

            store.Save(userId, creds);
            audit.LogOperation(userId, "UploadKubeConfig", true);

            return Ok(new { message = "Kubeconfig cargado correctamente" });
        }

        [HttpDelete("config/{userId}")]
        public IActionResult DeleteConfig(string userId)
        {
            var userCheck = UserExistance(userId);
            if (userCheck != null)
            {
                audit.LogOperation(userId, "DeleteConfig", false, "Usuario no encontrado");
                return userCheck;
            }

            store.Remove(userId);
            audit.LogOperation(userId, "DeleteConfig", true);

            return Ok(new { message = "Configuración eliminada correctamente" });
        }

        [HttpGet("pods/{userId}")]
        public async Task<IActionResult> GetPods(string userId, string ns = "default")
        {
            var userCheck = UserExistance(userId);
            if (userCheck != null)
            {
                audit.LogOperation(userId, "GetPods", false, "Usuario no encontrado");
                return userCheck;
            }

            try
            {
                var client = clientConnection.GetClient(userId);
                var pods = await client.CoreV1.ListNamespacedPodAsync(ns);
                var podNames = pods.Items.Select(p => p.Metadata.Name);
                audit.LogOperation(userId, "GetPods", true);
                return Ok(podNames);
            }
            catch (KeyNotFoundException ex)
            {
                audit.LogOperation(userId, "GetPods", false, ex.Message);
                return StatusCode(404, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                audit.LogOperation(userId, "GetPods", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("namespaces/{userId}")]
        public async Task<IActionResult> GetNamespaces(string userId)
        {
            var userCheck = UserExistance(userId);
            if (userCheck != null)
            {
                audit.LogOperation(userId, "GetNamespaces", false, "Usuario no encontrado");
                return userCheck;
            }

            try
            {
                var client = clientConnection.GetClient(userId);
                var namespaces = await client.CoreV1.ListNamespaceAsync();
                var nsNames = namespaces.Items.Select(n => n.Metadata.Name);
                audit.LogOperation(userId, "GetNamespaces", true);
                return Ok(nsNames);
            }
            catch (KeyNotFoundException ex)
            {
                audit.LogOperation(userId, "GetNamespaces", false, ex.Message);
                return StatusCode(404, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                audit.LogOperation(userId, "GetNamespaces", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("services/{ns}/{userId}")]
        public async Task<IActionResult> GetServices(string ns, string userId)
        {
            var userCheck = UserExistance(userId);
            if (userCheck != null)
            {
                audit.LogOperation(userId, "GetServices", false, "Usuario no encontrado");
                return userCheck;
            }

            try
            {
                var client = clientConnection.GetClient(userId);
                var services = await client.CoreV1.ListNamespacedServiceAsync(ns);
                var servicesNames = services.Items.Select(s => s.Metadata.Name);
                audit.LogOperation(userId, "GetServices", true);
                return Ok(servicesNames);
            }
            catch (KeyNotFoundException ex)
            {
                audit.LogOperation(userId, "GetServices", false, ex.Message);
                return StatusCode(404, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                audit.LogOperation(userId, "GetServices", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private IActionResult? UserExistance(string userId)
        {
            if (!store.Exists(userId))
                return StatusCode(404, new { message = $"No se encontraron credenciales para el usuario {userId}" });
            return null;
        }
    }
}
