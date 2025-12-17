using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Core;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Pagination;
using Servidor.KubeOrchestratorPlatform.Models.Enums;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Core
{
    public class SecretService : ISecretService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IClusterService clusterService;
        private readonly IAuditService auditService;

        public SecretService(
            IKubernetesUserClientConnection clientConnection,
            IClusterService clusterService,
            IAuditService auditService)
        {
            this.clientConnection = clientConnection;
            this.clusterService = clusterService;
            this.auditService = auditService;
        }

        public async Task CreateAsync(int userId, string clusterId, CreateSecretDTO dto)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);
            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                var secret = new V1Secret
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = dto.Name,
                        NamespaceProperty = dto.Namespace,
                        Labels = dto.Labels
                    },
                    Type = GetK8sSecretType(dto.Type),
                    StringData = dto.StringData
                };

                await client.CoreV1.CreateNamespacedSecretAsync(secret, dto.Namespace);
                auditService.LogOperation(userName, clusterName, $"CreateSecret: {dto.Name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateSecret: {dto.Name}", false, ex.Message);
                throw;
            }
        }

        private string GetK8sSecretType(SecretType type)
        {
            return type switch
            {
                SecretType.Opaque => "Opaque",
                SecretType.TlsSecret => "kubernetes.io/tls",
                SecretType.DockerConfigJson => "kubernetes.io/dockerconfigjson",
                SecretType.BasicAuth => "kubernetes.io/basic-auth",
                SecretType.SshAuth => "kubernetes.io/ssh-auth",
                SecretType.ServiceAccountToken => "kubernetes.io/service-account-token",
                SecretType.BootstrapToken => "bootstrap.kubernetes.io/token",
                _ => "Opaque"
            };
        }

        public async Task DeleteAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);
            try
            {
                var client = clientConnection.GetClient(userId, clusterId);
                await client.CoreV1.DeleteNamespacedSecretAsync(name, namespaceName);
                auditService.LogOperation(userName, clusterName, $"DeleteSecret: {name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"DeleteSecret: {name}", false, ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetNamesAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);

            var list = await client.CoreV1.ListNamespacedSecretAsync(namespaceName);

            return list.Items.Select(c => c.Metadata.Name);
        }

        public async Task<PagedResult<V1Secret>> GetPagedAsync(int userId, string clusterId, string namespaceName, int limit = 10, string? continueToken = null)
        {
            var client = clientConnection.GetClient(userId, clusterId);

            var response = await client.CoreV1.ListNamespacedSecretAsync(
                namespaceName,
                limit: limit,
                continueParameter: continueToken
            );

            foreach (var item in response.Items)
            {
                if (item.Metadata != null) item.Metadata.ManagedFields = null;
            }

            return new PagedResult<V1Secret>
            {
                Items = response.Items,
                ContinueToken = response.Metadata.ContinueProperty,
                RemainingCount = (int)(response.Metadata.RemainingItemCount ?? 0)
            };
        }
    }
}