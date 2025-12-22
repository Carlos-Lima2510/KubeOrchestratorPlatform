using k8s;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Resources
{
    public class ResourceService : IResourceService
    {
        private readonly IKubernetesUserClientConnection _connection;

        public ResourceService(IKubernetesUserClientConnection connection)
        {
            _connection = connection;
        }

        public async Task<string> GetResourceYamlAsync(int userId, string clusterId, string ns, string type, string name)
        {
            var client = _connection.GetClient(userId, clusterId);
            object? resource = null;

            switch (type.ToLower())
            {
                case "pod":
                    resource = await client.CoreV1.ReadNamespacedPodAsync(name, ns);
                    break;
                case "service":
                    resource = await client.CoreV1.ReadNamespacedServiceAsync(name, ns);
                    break;
                case "ingress":
                    resource = await client.NetworkingV1.ReadNamespacedIngressAsync(name, ns);
                    break;
                default:
                    return "--- ERROR ---\nTipo de recurso no soportado para exportación YAML.";
            }

            if (resource == null) return "--- ERROR ---\nRecurso no encontrado.";

            return KubernetesYaml.Serialize(resource);
        }
    }
}