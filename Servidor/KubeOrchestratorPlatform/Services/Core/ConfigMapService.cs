using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Core;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Pagination;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Core
{
    public class ConfigMapService : IConfigMapService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IClusterService clusterService;
        private readonly IAuditService auditService;

        public ConfigMapService(
            IKubernetesUserClientConnection clientConnection,
            IClusterService clusterService,
            IAuditService auditService)
        {
            this.clientConnection = clientConnection;
            this.clusterService = clusterService;
            this.auditService = auditService;
        }
        public async Task CreateAsync(int userId, string clusterId, CreateConfigMapDTO dto)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);
            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                var configMap = new V1ConfigMap
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = dto.Name,
                        NamespaceProperty = dto.Namespace,
                        Labels = dto.Labels
                    },
                    Data = dto.Data
                };

                await client.CoreV1.CreateNamespacedConfigMapAsync(configMap, dto.Namespace);
                auditService.LogOperation(userName, clusterName, $"CreateConfigMap: {dto.Name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateConfigMap: {dto.Name}", false, ex.Message);
                throw;
            }
        }

        public async Task DeleteAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);
            try
            {
                var client = clientConnection.GetClient(userId, clusterId);
                await client.CoreV1.DeleteNamespacedConfigMapAsync(name, namespaceName);
                auditService.LogOperation(userName, clusterName, $"DeleteConfigMap: {name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"DeleteConfigMap: {name}", false, ex.Message);
                throw;
            }
        }
        public async Task<IEnumerable<string>> GetNamesAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);

            var list = await client.CoreV1.ListNamespacedConfigMapAsync(namespaceName);

            return list.Items.Select(c => c.Metadata.Name);
        }
        public async Task<PagedResult<V1ConfigMap>> GetPagedAsync(
            int userId,
            string clusterId,
            string namespaceName,
            int limit = 50,
            string? continueToken = null)
        {
            var client = clientConnection.GetClient(userId, clusterId);

            var response = await client.CoreV1.ListNamespacedConfigMapAsync(
                namespaceName,
                limit: limit,
                continueParameter: continueToken
            );

            foreach (var item in response.Items)
            {
                if (item.Metadata != null) item.Metadata.ManagedFields = null;
            }

            return new PagedResult<V1ConfigMap>
            {
                Items = response.Items,
                ContinueToken = response.Metadata.ContinueProperty,
                RemainingCount = (int)(response.Metadata.RemainingItemCount ?? 0)
            };
        }
    }
}