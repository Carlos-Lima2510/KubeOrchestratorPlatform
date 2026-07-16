using Servidor.KubeOrchestratorPlatform.Models.DTOs;

namespace Servidor.KubeOrchestratorPlatform.Services.Metrics
{
    public interface IMetricsService
    {
        Task<IEnumerable<ClusterMetricsDTO>> GetClusterMetricsAsync(int userId, string clusterId);
        Task<IDictionary<string, PodMetricsDTO>> GetPodMetricsAsync(int userId, string clusterId, string namespaceName);
    }
}