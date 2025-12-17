using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Core;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Pagination;

namespace Servidor.KubeOrchestratorPlatform.Services.Core
{
    public interface ISecretService
    {
        Task<IEnumerable<string>> GetNamesAsync(int userId, string clusterId, string namespaceName);
        Task CreateAsync(int userId, string clusterId, CreateSecretDTO dto);
        Task DeleteAsync(int userId, string clusterId, string namespaceName, string name);
        Task <PagedResult<V1Secret>> GetPagedAsync(int userId, string clusterId, string namespaceName, int limit = 10, string? continueToken = null);
    }
}