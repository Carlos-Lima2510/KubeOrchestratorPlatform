using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Namespace
{
    public class NamespaceService : INamespaceService
    {
        private readonly IKubernetesUserClientConnection connection;
        private readonly IAuditService auditService;
        private readonly IClusterService clusterService;
        public NamespaceService(IKubernetesUserClientConnection connection, IAuditService auditService, IClusterService clusterService)
        {
            this.connection = connection;
            this.auditService = auditService;
            this.clusterService = clusterService;
        }
        public async Task CreateAsync(int userId, string clusterId, string name)
        {
            var (userName, clusterName) = GetAuditContext(userId, clusterId);

            try
            {
                var client = connection.GetClient(userId, clusterId);

                var ns = new V1Namespace
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = name,
                        Labels = new Dictionary<string, string>
                        {
                            { "managed-by", "kube-orchestrator" },
                            { "created-by", userName }
                        }
                    }
                };

                await client.CoreV1.CreateNamespaceAsync(ns);

                auditService.LogOperation(userName, clusterName, $"CreateNamespace: {name}", true);
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateNamespace: {name}", false, ex.Message);
                throw;
            }
        }

        public async Task DeleteAsync(int userId, string clusterId, string name)
        {
            var (userName, clusterName) = GetAuditContext(userId, clusterId);

            try
            {
                var client = connection.GetClient(userId, clusterId);
                await client.CoreV1.DeleteNamespaceAsync(name);

                auditService.LogOperation(userName, clusterName, $"DeleteNamespace: {name}", true);
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                auditService.LogOperation(userName, clusterName, $"DeleteNamespace: {name}", false, ex.Message);
                throw;
            }
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

        public async Task<NamespaceDTO?> GetByNameAsync(int userId, string clusterId, string clusterName)
        {
            var client = connection.GetClient(userId, clusterId);

            try
            {
                var ns = await client.CoreV1.ReadNamespaceAsync(clusterName);

                return new NamespaceDTO
                {
                    Name = ns.Metadata.Name,
                    Status = ns.Status.Phase,
                    CreatedAt = ns.Metadata.CreationTimestamp,
                    Labels = ns.Metadata.Labels != null ? new Dictionary<string, string>(ns.Metadata.Labels) : new Dictionary<string, string>()
                };
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null!;
            }
        }

        private (string UserName, string ClusterName) GetAuditContext(int userId, string clusterId)
        {
            return clusterService.GetClusterContext(userId, clusterId);
        }
    }
}