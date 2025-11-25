using k8s;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Store;

namespace Servidor.KubeOrchestratorPlatform.Services.Connection.Decorators
{
    public class LoggingKubernetesUserClientConnection : IKubernetesUserClientConnection
    {
        private readonly IKubernetesUserClientConnection innerClass;
        private readonly IAuditService auditService;
        private readonly IUserClusterStore store;

        public LoggingKubernetesUserClientConnection(IKubernetesUserClientConnection innerClass, IAuditService auditService, IUserClusterStore store)
        {
            this.innerClass = innerClass;
            this.auditService = auditService;
            this.store = store;
        }

        public IKubernetes GetClient(int userId, string clusterId)
        {
            var clusterConfig = store.GetClusterWithUser(userId, clusterId);
            var userName = clusterConfig?.User?.Username ?? "";
            var clusterName = clusterConfig?.DisplayName ?? clusterId;

            try
            {
                var client = innerClass.GetClient(userId, clusterId);
                auditService.LogConnection(userName, clusterName, true);
                return client;
            }
            catch (Exception ex)
            {
                auditService.LogConnection(userName, clusterName, false, ex.Message);
                throw;
            }
        }
    }
}