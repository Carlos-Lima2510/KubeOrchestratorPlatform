using k8s;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Services.Parser;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Store;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KubeTestController : ControllerBase
    {
        private readonly IKubeConfigParser _parser;
        private readonly IUserClusterStore store;
        private readonly IKubernetesUserClientConnection clientConnection;

        public KubeTestController(
            IKubeConfigParser parser,
            IUserClusterStore store,
            IKubernetesUserClientConnection clientConnection)
        {
            _parser = parser;
            this.store = store;
            this.clientConnection = clientConnection;
        }

        // Endpoint para subir kubeconfig
        [HttpPost("upload-kubeconfig/{userId}")]
        public IActionResult UploadKubeConfig(string userId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo kubeconfig vacío.");

            using var reader = new StreamReader(file.OpenReadStream());
            var yaml = reader.ReadToEnd();

            var parsed = _parser.Parse(yaml);

            Console.WriteLine("Valores parseados: " + parsed.AuthType);

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

            return Ok(new { message = "Kubeconfig cargado correctamente" });
        }

        [HttpGet("pods/{userId}")]
        public async Task<IActionResult> GetPods(string userId, string ns = "default")
        {
            if (!store.Exists(userId))
                return NotFound($"No se encontraron credenciales para el usuario {userId}");

            var client = clientConnection.GetClient(userId);

            try
            {
                var pods = await client.CoreV1.ListNamespacedPodAsync(ns);
                var podNames = pods.Items.Select(p => p.Metadata.Name);
                return Ok(podNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("namespaces/{userId}")]
        public async Task<IActionResult> GetNamespaces(string userId)
        {
            if (!store.Exists(userId))
                return NotFound($"No se encontraron credenciales para el usuario {userId}");

            var client = clientConnection.GetClient(userId);

            try
            {
                var namespaces = await client.CoreV1.ListNamespaceAsync();
                var nsNames = namespaces.Items.Select(n => n.Metadata.Name);
                return Ok(nsNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("services/{ns}/{userId}")]
        public async Task<IActionResult> GetServices(string ns, string userId)
        {
            if (!store.Exists(userId))
                return NotFound($"No se encontraron credenciales para el usuario {userId}");

            var client = clientConnection.GetClient(userId);

            try
            {
                var services = await client.CoreV1.ListNamespacedServiceAsync(ns);
                var servicesNames = services.Items.Select(s => s.Metadata.Name);
                return Ok(servicesNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
