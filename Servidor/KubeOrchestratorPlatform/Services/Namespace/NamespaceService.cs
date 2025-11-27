using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Namespace
{
    public class NamespaceService : INamespaceService
    {
        private readonly IKubernetesUserClientConnection connection;
        public NamespaceService(IKubernetesUserClientConnection connection)
        {
            this.connection = connection;
        }
        public async Task CreateAsync(int userId, string clusterId, string name)
        {
            var client = connection.GetClient(userId, clusterId);

            var ns = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = new Dictionary<string, string> 
                    { 
                        { "managed-by", "kube-orchestrator" } 
                    }
                }
            };

            await client.CoreV1.CreateNamespaceAsync(ns);
        }

        public async Task DeleteAsync(int userId, string clusterId, string name)
        {
            var client = connection.GetClient(userId, clusterId);

            await client.CoreV1.DeleteNamespaceAsync(name);
        }

        public async Task<IEnumerable<NamespaceDTO>> GetAllAsync(int userId, string clusterId)
        {
            var client = connection.GetClient(userId, clusterId);

            var list = await client.CoreV1.ListNamespaceAsync();

            return list.Items.Select(ns => new NamespaceDTO
            {
                Name = ns.Metadata.Name,
                Status = ns.Status.Phase,
                CreatedAt = ns.Metadata.CreationTimestamp,
                Labels = ns.Metadata.Labels != null ? new Dictionary<string, string>(ns.Metadata.Labels) : new Dictionary<string, string>()
            });
        }
    }
}