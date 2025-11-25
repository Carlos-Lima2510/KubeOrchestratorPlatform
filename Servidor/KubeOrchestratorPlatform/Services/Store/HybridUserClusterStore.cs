using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Services.Store.Cache;
using Servidor.KubeOrchestratorPlatform.Services.Store.Repositories;

namespace Servidor.KubeOrchestratorPlatform.Services.Store
{
    public class HybridUserClusterStore : IUserClusterStore
    {
        private readonly IUserClusterRepository repository;
        private readonly IUserClusterCache cache;

        public HybridUserClusterStore(IUserClusterRepository repository, IUserClusterCache cache)
        {
            this.repository = repository;
            this.cache = cache;
        }

        public bool Exists(int userId, string clusterId)
        {
            if (cache.Exists(userId, clusterId)) return true;
            return repository.Exists(userId, clusterId);
        }

        public ClusterCredentials? Get(int userId, string clusterId)
        {
            var cached = cache.Get(userId, clusterId);
            if (cached != null) return cached;

            var config = repository.GetClusterConfiguration(userId, clusterId);
            if (config != null)
            {
                cache.Set(userId, clusterId, config.Credentials);
                return config.Credentials;
            }

            return null;
        }

        public void Remove(int userId, string clusterId)
        {
            repository.RemoveClusterConfiguration(userId, clusterId);
            cache.Remove(userId, clusterId);
        }

        public void Save(int userId, string clusterId, ClusterCredentials credentials)
        {
            var config = repository.GetClusterConfiguration(userId, clusterId);
            if (config == null)
            {
                config = new ConnectedCluster
                {
                    UserId = userId,
                    ClusterId = clusterId,
                    Credentials = credentials,
                    ServerUrl = credentials.Server ?? ""
                };
            }
            else
            {
                config.Credentials = credentials;
                config.ServerUrl = credentials.Server ?? "";
            }

            repository.SaveClusterConfiguration(config);

            cache.Set(userId, clusterId, credentials);
        }
    }
}