using k8s;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Servidor.KubeOrchestratorPlatform.Services.Connection
{
    public class KubernetesUserClientConnection : IKubernetesUserClientConnection
    {
        private readonly IClusterService store;
        private readonly IKubernetesClientFactory factory;
        private readonly IMemoryCache memoryCache;

        public KubernetesUserClientConnection(
            IClusterService store, 
            IKubernetesClientFactory factory, 
            IMemoryCache memoryCache)
        {
            this.store = store;
            this.factory = factory;
            this.memoryCache = memoryCache;
        }

        public IKubernetes GetClient(int userId, string clusterId)
        {
            var cacheKey = $"k8s_client_{userId}_{clusterId}";

            if (memoryCache.TryGetValue(cacheKey, out IKubernetes? cachedClient) && cachedClient != null)
            {
                return cachedClient;
            }

            var clusterConfig = store.GetCluster(userId, clusterId);

            if (clusterConfig == null)
                throw new Exception($"El cluster {clusterId} no fue encontrado para el usuario {userId}");
            
            if (string.IsNullOrEmpty(clusterConfig.ServerUrl))
                throw new Exception("La configuración del cluster no tiene una ServerUrl válida.");

            if (clusterConfig.Credentials == null)
                throw new Exception("El cluster existe pero no tiene credenciales asociadas.");

            var newClient = factory.CreateClient(new ParsedKubeConfig
            {
                Server = clusterConfig.ServerUrl, 

                CertificateAuthorityData = clusterConfig.Credentials.CertificateAuthorityData,
                Token = clusterConfig.Credentials.Token,
                ClientCertificateData = clusterConfig.Credentials.ClientCertificateData,
                ClientKeyData = clusterConfig.Credentials.ClientKeyData,
                AuthType = clusterConfig.Credentials.AuthType
            });

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .RegisterPostEvictionCallback((key, value, reason, state) => 
                {
                    if (value is IDisposable disposableClient)
                    {
                        disposableClient.Dispose();
                    }
                });
            
            memoryCache.Set(cacheKey, newClient, cacheOptions);

            return newClient;
        }
    }
}