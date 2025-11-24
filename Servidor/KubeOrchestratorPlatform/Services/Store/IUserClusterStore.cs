using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store
{
    public interface IUserClusterStore
    {
        void Save(int userId, string clusterId, UserClusterCredentials credentials);
        UserClusterCredentials? Get(int userId, string clusterId);
        bool Exists(int userId, string clusterId);
        void Remove(int userId, string clusterId);
    }
}