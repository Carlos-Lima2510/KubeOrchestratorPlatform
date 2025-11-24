using k8s;
using Servidor.KubeOrchestratorPlatform.Services.Audit;

namespace Servidor.KubeOrchestratorPlatform.Services.Connection.Decorators
{
    public class LoggingKubernetesUserClientConnection : IKubernetesUserClientConnection
    {
        private readonly IKubernetesUserClientConnection innerClass;
        private readonly IAuditService auditService;

        public LoggingKubernetesUserClientConnection(IKubernetesUserClientConnection innerClass, IAuditService auditService)
        {
            this.innerClass = innerClass;
            this.auditService = auditService;
        }

        public IKubernetes GetClient(int userId, string clusterId)
        {
            try
            {
                var client = innerClass.GetClient(userId, clusterId);
                auditService.LogConnection(userId.ToString(), true);
                return client;
            }
            catch (Exception ex)
            {
                auditService.LogConnection(userId.ToString(), false, ex.Message);
                throw;
            }
        }
    }
}