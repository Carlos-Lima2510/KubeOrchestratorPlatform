using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store
{
    public interface IUserClusterStore
    {
        void Save(int userId, string publicClusterId, ClusterCredentials credentials, string serverUrl, string displayName);
        ConnectedCluster? GetClusterWithUser(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
        void Remove(int userId, string clusterId);
    }
}