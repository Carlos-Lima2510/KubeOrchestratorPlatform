using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store.Cache
{
    public interface IUserClusterCache
    {
        void Set(int userId, string clusterId, ClusterCredentials credentials);
        void Remove(int userId, string clusterId);
        ClusterCredentials? Get(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
    }
}