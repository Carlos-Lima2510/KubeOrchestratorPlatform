using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store.Repositories
{
    public interface IUserClusterRepository
    {
        void SaveClusterConfiguration(ClusterConfiguration configuration);
        ClusterConfiguration? GetClusterConfiguration(int userId, string clusterId);
        void RemoveClusterConfiguration(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
    }
}