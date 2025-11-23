namespace Servidor.KubeOrchestratorPlatform.Services.Audit
{
    public interface IAuditService{
        void LogConnection(string userId, bool succes, string? error = null);
        void LogOperation(string userId, bool succes, string? error = null);
    }
}