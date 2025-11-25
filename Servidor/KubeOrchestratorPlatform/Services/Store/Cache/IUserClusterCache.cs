using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store.Cache
{
    public interface IUserClusterCache
    {
        void Set(int userId, string clusterId, ConnectedCluster credentials);
        void Remove(int userId, string clusterId);
        ConnectedCluster? Get(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
    }
}