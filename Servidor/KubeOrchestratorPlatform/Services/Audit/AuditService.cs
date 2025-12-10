namespace Servidor.KubeOrchestratorPlatform.Services.Audit
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> logger;
        public AuditService(ILogger<AuditService> logger)
        {
            this.logger = logger;
        }

        public void LogConnection(string userName, string clusterName, bool success, string? error = null)
        {
            if (success)
            {
                logger.LogDebug("Conexión exitosa al cluster para usuario {UserName} en el cluster {ClusterName}", userName, clusterName);
            }
            else
            {
                logger.LogWarning("Intento de conexión fallido para usuario {UserName} en cluster {ClusterName}. Error: {Error}", userName, clusterName, error);
            }
        }

        public void LogOperation(string userName, string clusterName, string operation, bool success, string? error = null)
        {
            if (success)
                logger.LogInformation("Usuario {UserName} ejecutó operación {Operation} en cluster {ClusterName} correctamente", userName, operation, clusterName);
            else
                logger.LogError("Usuario {UserName} falló en operación {Operation} en cluster {ClusterName}. Error: {Error}", userName, operation, clusterName, error);
        }
    }
}