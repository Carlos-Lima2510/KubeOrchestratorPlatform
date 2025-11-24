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

        public IKubernetes GetClient(string userId)
        {
            try
            {
                var client = innerClass.GetClient(userId);
                auditService.LogConnection(userId, true);
                return client;
            }
            catch (Exception ex)
            {
                auditService.LogConnection(userId, false, ex.Message);
                throw;
            }
        }
    }
}