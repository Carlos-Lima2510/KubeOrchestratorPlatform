namespace Servidor.KubeOrchestratorPlatform.Services.Audit
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> logger;
        public AuditService(ILogger<AuditService> logger)
        {
            this.logger = logger;
        }

        public void LogConnection(string userId, bool success, string? error = null)
        {
            if (success)
                logger.LogInformation("Conexión exitosa al cluster para usuario {UserId}", userId);
            else
                logger.LogWarning("Intento de conexión fallido para usuario {UserId}. Error: {Error}", userId, error);
        }

        public void LogOperation(string userId, string operation, bool success, string? error = null)
        {
            if (success)
                logger.LogInformation("Usuario {UserId} ejecutó operación {Operation} correctamente", userId, operation);
            else
                logger.LogError("Usuario {UserId} falló en operación {Operation}. Error: {Error}", userId, operation, error);
        }
    }
}