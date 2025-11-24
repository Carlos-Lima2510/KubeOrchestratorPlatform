using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store.Cache
{
    public interface IUserClusterCache
    {
        void Set(int userId, string clusterId, UserClusterCredentials credentials);
        void Remove(int userId, string clusterId);
        UserClusterCredentials? Get(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
    }
}