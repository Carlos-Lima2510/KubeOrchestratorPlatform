using k8s;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Factories;
using Servidor.KubeOrchestratorPlatform.Repositories;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KubeTestController : ControllerBase
    {
        private readonly IKubeConfigParser _parser;
        private readonly IKubernetesClientFactory _factory;
        private readonly IUserClusterStore _store;

        public KubeTestController(
            IKubeConfigParser parser,
            IKubernetesClientFactory factory,
            IUserClusterStore store)
        {
            _parser = parser;
            _factory = factory;
            _store = store;
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

            var creds = new UserClusterCredentials
            {
                Server = parsed.Server,
                CertificateAuthorityData = parsed.CertificateAuthorityData,
                Token = parsed.Token,
                ClientCertificateData = parsed.ClientCertificateData,
                ClientKeyData = parsed.ClientKeyData,
                AuthType = parsed.AuthType
            };

            _store.Save(userId, creds);

            return Ok(new { message = "Kubeconfig cargado correctamente" });
        }

        [HttpGet("pods/{userId}")]
        public async Task<IActionResult> GetPods(string userId, string ns = "default")
        {
            if (!_store.Exists(userId))
                return NotFound($"No se encontraron credenciales para el usuario {userId}");

            var creds = _store.Get(userId);

            var client = _factory.CreateClient(new ParsedKubeConfig
            {
                Server = creds.Server,
                CertificateAuthorityData = creds.CertificateAuthorityData,
                Token = creds.Token,
                ClientCertificateData = creds.ClientCertificateData,
                ClientKeyData = creds.ClientKeyData,
                AuthType = creds.AuthType
            });

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
            if (!_store.Exists(userId))
                return NotFound($"No se encontraron credenciales para el usuario {userId}");

            var creds = _store.Get(userId);

            var client = _factory.CreateClient(new ParsedKubeConfig
            {
                Server = creds.Server,
                CertificateAuthorityData = creds.CertificateAuthorityData,
                Token = creds.Token,
                ClientCertificateData = creds.ClientCertificateData,
                ClientKeyData = creds.ClientKeyData,
                AuthType = creds.AuthType
            });

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
            if (!_store.Exists(userId))
                return NotFound($"No se encontraron credenciales para el usuario {userId}");

            var creds = _store.Get(userId);

            var client = _factory.CreateClient(new ParsedKubeConfig
            {
                Server = creds.Server,
                CertificateAuthorityData = creds.CertificateAuthorityData,
                Token = creds.Token,
                ClientCertificateData = creds.ClientCertificateData,
                ClientKeyData = creds.ClientKeyData,
                AuthType = creds.AuthType
            });

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
