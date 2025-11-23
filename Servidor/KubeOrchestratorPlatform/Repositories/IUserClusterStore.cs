using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Repositories
{
    public interface IUserClusterStore
    {
        void Save(string userId, UserClusterCredentials credentials);
        UserClusterCredentials Get(string userId);
        bool Exists(string userId);
        void Remove(string userId);
    }
}