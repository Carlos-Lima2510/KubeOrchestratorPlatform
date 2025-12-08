using Servidor.KubeOrchestratorPlatform.Models.DTOs;

namespace Servidor.KubeOrchestratorPlatform.Services.Metrics
{
    public interface IMetricsService
    {
        Task<IEnumerable<ClusterMetricsDTO>> GetClusterMetricsAsync(int userId, string clusterId);
    }
}