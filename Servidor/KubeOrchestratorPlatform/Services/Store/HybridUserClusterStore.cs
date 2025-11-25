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

        public ConnectedCluster? GetClusterWithUser(int userId, string clusterId)
        {
            return repository.GetClusterConfiguration(userId, clusterId);
        }

        public void Remove(int userId, string clusterId)
        {
            repository.RemoveClusterConfiguration(userId, clusterId);
            cache.Remove(userId, clusterId);
        }
        public void Save(int userId, string publicClusterId, ClusterCredentials credentials, string serverUrl, string displayName)
        {
            var config = repository.GetClusterConfiguration(userId, publicClusterId);

            if (config == null)
            {
                config = new ConnectedCluster
                {
                    UserId = userId,
                    PublicClusterId = publicClusterId,
                    ServerUrl = serverUrl,
                    DisplayName = displayName,
                    IsActive = true,
                    Credentials = credentials
                };

                repository.SaveClusterConfiguration(config);
            }
            else
            {
                config.ServerUrl = serverUrl;
                config.DisplayName = displayName;

                if (config.Credentials == null)
                {
                    config.Credentials = credentials;
                }
                else
                {
                    config.Credentials.Token = credentials.Token;
                    config.Credentials.ClientCertificateData = credentials.ClientCertificateData;
                    config.Credentials.ClientKeyData = credentials.ClientKeyData;
                    config.Credentials.CertificateAuthorityData = credentials.CertificateAuthorityData;
                    config.Credentials.AuthType = credentials.AuthType;
                }

                repository.SaveClusterConfiguration(config); 
            }

            cache.Set(userId, publicClusterId, config.Credentials);
        }
    }
}