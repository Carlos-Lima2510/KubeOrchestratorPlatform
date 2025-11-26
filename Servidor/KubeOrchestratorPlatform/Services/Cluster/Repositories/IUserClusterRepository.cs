using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Cluster.Repositories
{
    public interface IUserClusterRepository
    {
        void SaveClusterConfiguration(ConnectedCluster configuration);
        ConnectedCluster? GetClusterConfiguration(int userId, string clusterId);
        void RemoveClusterConfiguration(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
    }
}