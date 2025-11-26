using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Services.Cluster.Cache;
using Servidor.KubeOrchestratorPlatform.Services.Cluster.Repositories;

namespace Servidor.KubeOrchestratorPlatform.Services.Cluster
{
    public class ClusterService : IClusterService
    {
        private readonly IUserClusterRepository repository;
        private readonly IUserClusterCache cache;

        public ClusterService(IUserClusterRepository repository, IUserClusterCache cache)
        {
            this.repository = repository;
            this.cache = cache;
        }
        public ConnectedCluster? GetCluster(int userId, string clusterId)
        {
            var cached = cache.Get(userId, clusterId);
            if (cached != null) 
            {
                return cached; 
            }

            var dbConfig = repository.GetClusterConfiguration(userId, clusterId);
            
            if (dbConfig != null)
            {
                cache.Set(userId, clusterId, dbConfig);
            }

            return dbConfig;
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
            }
            else
            {
                config.ServerUrl = serverUrl;
                config.DisplayName = displayName;

                if (config.Credentials == null) config.Credentials = credentials;
                else 
                {
                    config.Credentials.Token = credentials.Token;
                    config.Credentials.ClientCertificateData = credentials.ClientCertificateData;
                    config.Credentials.ClientKeyData = credentials.ClientKeyData;
                    config.Credentials.CertificateAuthorityData = credentials.CertificateAuthorityData;
                    config.Credentials.AuthType = credentials.AuthType;
                }
            }

            repository.SaveClusterConfiguration(config);

            cache.Set(userId, publicClusterId, config);
        }
        public void Remove(int userId, string clusterId)
        {
            repository.RemoveClusterConfiguration(userId, clusterId);
            cache.Remove(userId, clusterId);
        }
        public bool Exists(int userId, string clusterId)
        {
            if (cache.Exists(userId, clusterId)) return true;
            return repository.Exists(userId, clusterId);
        }
    }
}