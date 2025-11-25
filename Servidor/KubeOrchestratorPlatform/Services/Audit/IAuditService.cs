namespace Servidor.KubeOrchestratorPlatform.Services.Audit
{
    public interface IAuditService{
        void LogConnection(string userName, string clusterName, bool succes, string? error = null);
        void LogOperation(string userName, string clusterName, string operation, bool succes, string? error = null);
    }
}