using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments;

namespace Servidor.KubeOrchestratorPlatform.Services.Deployment
{
    public interface IDeploymentService
    {
        Task<IEnumerable<V1Deployment>> GetAllAsync(int userId, string clusterId, string namespaceName);
        Task<V1Deployment?> GetByNameAsync(int userId, string clusterId, string namespaceName, string name);
        Task CreateAsync(int userId, string clusterId, CreateDeploymentDTO dto);
        Task DeleteAsync(int userId, string clusterId, string namespaceName, string name);
    }
}   