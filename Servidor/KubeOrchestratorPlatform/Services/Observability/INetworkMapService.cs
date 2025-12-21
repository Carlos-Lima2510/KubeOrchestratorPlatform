using Servidor.KubeOrchestratorPlatform.Models.DTOs.Observability;

namespace Servidor.KubeOrchestratorPlatform.Services.Observability
{
    public interface INetworkMapService
    {
        Task<NetworkGraphDTO> GetNetworkMapAsync(int userId, string clusterId, string namespaceName);
    }
}
