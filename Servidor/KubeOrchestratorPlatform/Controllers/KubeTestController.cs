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

        // --- SUBIDA DE ARCHIVO (Ya corregido) ---
        [HttpPost("upload-kubeconfig/{userId}/{clusterId}")]
        public async Task<IActionResult> UploadKubeConfig(int userId, string clusterId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _audit.LogOperation(userId.ToString(), clusterId, "UploadKubeConfig", false, "Archivo vacío");
                return BadRequest("Archivo kubeconfig vacío.");
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var yaml = await reader.ReadToEndAsync();
            
            try 
            {
                var parsed = _parser.Parse(yaml);
                
                var creds = new ClusterCredentials
                {
                    CertificateAuthorityData = parsed.CertificateAuthorityData,
                    Token = parsed.Token,
                    ClientCertificateData = parsed.ClientCertificateData,
                    ClientKeyData = parsed.ClientKeyData,
                    AuthType = parsed.AuthType
                };

                var displayName = parsed.ClusterName ?? clusterId;
                
                // Nota: Asegúrate de que tu Store acepte 'int' userId
                _store.Save(userId, clusterId, creds, parsed.Server, displayName);

                _audit.LogOperation(parsed.UserName ?? "User", displayName, "UploadKubeConfig", true);
                return Ok(new { message = "Kubeconfig cargado correctamente", server = parsed.Server });
            }
            catch (Exception ex)
            {
                _audit.LogOperation(userId.ToString(), clusterId, "UploadKubeConfig", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // --- ELIMINAR CONFIGURACIÓN ---
        [HttpDelete("config/{userId}/{clusterId}")]
        public IActionResult DeleteConfig(int userId, string clusterId)
        {
            // Usamos el helper privado para validar y obtener contexto
            var context = ValidateAndGetContext(userId, clusterId, "DeleteConfig");
            if (context.ErrorResult != null) return context.ErrorResult;

            try
            {
                _store.Remove(userId, clusterId);
                _audit.LogOperation(context.UserName, context.ClusterName, $"DeleteConfig-{clusterId}", true);
                return Ok(new { message = "Configuración eliminada correctamente" });
            }
            catch (Exception ex)
            {
                 // Log de error inesperado
                 return StatusCode(500, new { error = ex.Message });
            }
        }

        // --- OBTENER PODS ---
        [HttpGet("pods/{userId}/{clusterId}/{ns}")]
        public async Task<IActionResult> GetPods(int userId, string clusterId, string ns)
        {
            var context = ValidateAndGetContext(userId, clusterId, "GetPods");
            if (context.ErrorResult != null) return context.ErrorResult;

            try
            {
                // Obtenemos el cliente K8s ya autenticado
                var client = _clientConnection.GetClient(userId, clusterId);
                
                // Llamada a Kubernetes
                var pods = await client.CoreV1.ListNamespacedPodAsync(ns);
                
                // Proyección de datos (Select)
                var podList = pods.Items.Select(p => new {
                    Name = p.Metadata.Name,
                    Status = p.Status.Phase,
                    IP = p.Status.PodIP,
                    StartTime = p.Status.StartTime
                });

                _audit.LogOperation(context.UserName, context.ClusterName, $"GetPods-{clusterId}", true);
                return Ok(podList);
            }
            catch (Exception ex)
            {
                _audit.LogOperation(context.UserName, context.ClusterName, $"GetPods-{clusterId}", false, ex.Message);

                if (ex is k8s.Autorest.HttpOperationException httpEx && httpEx.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                     return NotFound(new { message = $"Namespace '{ns}' no encontrado en el cluster." });
                }
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // --- OBTENER NAMESPACES ---
        [HttpGet("namespaces/{userId}/{clusterId}")]
        public async Task<IActionResult> GetNamespaces(int userId, string clusterId)
        {
            var context = ValidateAndGetContext(userId, clusterId, "GetNamespaces");
            if (context.ErrorResult != null) return context.ErrorResult;

            try
            {
                var client = _clientConnection.GetClient(userId, clusterId);
                var namespaces = await client.CoreV1.ListNamespaceAsync();
                
                var nsList = namespaces.Items.Select(n => new {
                    Name = n.Metadata.Name,
                    Status = n.Status.Phase
                });

                _audit.LogOperation(context.UserName, context.ClusterName, $"GetNamespaces-{clusterId}", true);
                return Ok(nsList);
            }
            catch (Exception ex)
            {
                _audit.LogOperation(context.UserName, context.ClusterName, $"GetNamespaces-{clusterId}", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // --- OBTENER SERVICIOS ---
        [HttpGet("services/{userId}/{clusterId}/{ns}")]
        public async Task<IActionResult> GetServices(int userId, string clusterId, string ns)
        {
            var context = ValidateAndGetContext(userId, clusterId, "GetServices");
            if (context.ErrorResult != null) return context.ErrorResult;

            try
            {
                var client = _clientConnection.GetClient(userId, clusterId);
                var services = await client.CoreV1.ListNamespacedServiceAsync(ns);
                
                var svcList = services.Items.Select(s => new {
                    Name = s.Metadata.Name,
                    Type = s.Spec.Type,
                    ClusterIP = s.Spec.ClusterIP,
                    Ports = s.Spec.Ports.Select(p => $"{p.Port}:{p.TargetPort}/{p.Protocol}")
                });

                _audit.LogOperation(context.UserName, context.ClusterName, $"GetServices-{clusterId}", true);
                return Ok(svcList);
            }
            catch (Exception ex)
            {
                _audit.LogOperation(context.UserName, context.ClusterName, $"GetServices-{clusterId}", false, ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================================
        // HELPER PRIVADO: Evita repetir código de validación
        // ==========================================================
        private (IActionResult? ErrorResult, string UserName, string ClusterName) ValidateAndGetContext(int userId, string clusterId, string operationName)
        {
            var clusterConfig = _store.GetClusterWithUser(userId, clusterId);
            
            if (clusterConfig == null)
            {
                _audit.LogOperation(userId.ToString(), clusterId, $"{operationName}-{clusterId}", false, "Usuario o cluster no encontrado");
                return (NotFound(new { message = "Usuario o cluster no encontrado" }), "", "");
            }

            var userName = clusterConfig.User?.Username ?? "Unknown";
            var clusterName = clusterConfig.DisplayName ?? clusterId;

            return (null, userName, clusterName);
        }
    }
}