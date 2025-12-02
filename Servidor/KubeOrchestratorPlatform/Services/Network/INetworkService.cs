using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network;
using Servidor.KubeOrchestratorPlatform.Responses;

namespace Servidor.KubeOrchestratorPlatform.Services.Network
{
    public interface INetworkService
    {
        Task<IEnumerable<V1Service>> GetAllAsync(int userId, string clusterId, string namespaceName);
        Task<V1Service?> GetByNameAsync(int userId, string clusterId, string namespaceName, string name);
        Task<NetworkServiceResponse> CreateAsync(int userId, string clusterId, CreateServiceDTO dto);
        Task DeleteAsync(int userId, string clusterId, string namespaceName, string name);

    }
}