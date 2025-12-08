using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Pods;

namespace Servidor.KubeOrchestratorPlatform.Services.Pods
{
    public interface IPodService
    {
        Task<IEnumerable<PodDTO>> GetByDeploymentAsync(int userId, string clusterId, string namespaceName, string deploymentName);
        Task<Stream> GetLogsAsync(int userId, string clusterId, string namespaceName, string podName, string containerName);
    }
}