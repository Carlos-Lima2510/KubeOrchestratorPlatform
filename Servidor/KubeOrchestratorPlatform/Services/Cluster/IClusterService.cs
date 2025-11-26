using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Cluster
{
    public interface IClusterService
    {
        void Save(int userId, string publicClusterId, ClusterCredentials credentials, string serverUrl, string displayName);
        ConnectedCluster? GetCluster(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
        void Remove(int userId, string clusterId);
    }
}