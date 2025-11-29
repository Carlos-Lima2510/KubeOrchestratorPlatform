using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments;

namespace Servidor.KubeOrchestratorPlatform.Services.Deployment
{
    public interface IDeploymentService
    {
        /// <summary>
        /// Obtiene todos los deployments de un namespace específico.
        /// </summary>
        Task<IEnumerable<V1Deployment>> GetAllAsync(int userId, string clusterId, string namespaceName);

        /// <summary>
        /// Obtiene un deployment específico por nombre. Retorna null si no existe.
        /// </summary>
        Task<V1Deployment?> GetByNameAsync(int userId, string clusterId, string namespaceName, string name);

        /// <summary>
        /// Crea un deployment completo con soporte para límites, probes, volúmenes, etc.
        /// </summary>
        Task CreateAsync(int userId, string clusterId, CreateDeploymentDTO dto);

        /// <summary>
        /// Elimina un deployment del clúster.
        /// </summary>
        Task DeleteAsync(int userId, string clusterId, string namespaceName, string name);
    }
}   