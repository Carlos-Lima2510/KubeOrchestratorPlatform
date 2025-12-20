using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network.Ingress;
using Servidor.KubeOrchestratorPlatform.Responses;

namespace Servidor.KubeOrchestratorPlatform.Services.Ingress
{
    public interface IIngressService
    {
        Task<IEnumerable<V1Ingress>> GetAllAsync(int userId, string clusterId, string namespaceName);
        Task<IngressServiceResponse> CreateAsync(int userId, string clusterId, CreateIngressDTO dto);
        Task DeleteAsync(int userId, string clusterId, string namespaceName, string name);
    }
}
