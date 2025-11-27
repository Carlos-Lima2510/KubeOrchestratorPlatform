using Servidor.KubeOrchestratorPlatform.Models.DTOs;

namespace Servidor.KubeOrchestratorPlatform.Services.Namespace
{
    public interface INamespaceService
    {
        Task<IEnumerable<NamespaceDTO>> GetAllAsync(int userId, string clusterId);
        Task CreateAsync(int userId, string clusterId, string name);
        Task DeleteAsync(int userId, string clusterId, string name);
        Task<NamespaceDTO?> GetByNameAsync(int userId, string clusterId, string clusterName);
    }
}