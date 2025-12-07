using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Repositories
{
    public interface IUserClusterRepository
    {
        Task<IEnumerable<ConnectedCluster>> GetAllClustersAsync(int userId);
        void SaveClusterConfiguration(ConnectedCluster configuration);
        ConnectedCluster? GetClusterConfiguration(int userId, string clusterId);
        void RemoveClusterConfiguration(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
        (string? UserName, string? ClusterName) GetClusterNames(int userId, string clusterId);
    }
}