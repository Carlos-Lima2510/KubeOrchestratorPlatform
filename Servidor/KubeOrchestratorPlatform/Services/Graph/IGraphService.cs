using Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph;

namespace Servidor.KubeOrchestratorPlatform.Services.Graph
{
    public interface IGraphService
    {
        Task<GraphDTO> GetFullTopologyAsync(int userId, string clusterId, string namespaceName);
    }
}