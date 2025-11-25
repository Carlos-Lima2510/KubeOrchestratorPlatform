using k8s;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Services.Store;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Connection
{
    public class KubernetesUserClientConnection : IKubernetesUserClientConnection
    {
        private readonly IUserClusterStore store;
        private readonly IKubernetesClientFactory factory;

        public KubernetesUserClientConnection(IUserClusterStore store, IKubernetesClientFactory factory)
        {
            this.store = store;
            this.factory = factory;
        }

        public IKubernetes GetClient(int userId, string clusterId)
        {
            var clusterConfig = store.GetClusterWithUser(userId, clusterId);

            if (clusterConfig == null)
                throw new Exception($"El cluster {clusterId} no fue encontrado para el usuario {userId}");
            
            if (string.IsNullOrEmpty(clusterConfig.ServerUrl))
                throw new Exception("La configuración del cluster no tiene una ServerUrl válida.");

            if (clusterConfig.Credentials == null)
                throw new Exception("El cluster existe pero no tiene credenciales asociadas.");

            return factory.CreateClient(new ParsedKubeConfig
            {
                Server = clusterConfig.ServerUrl, 

                CertificateAuthorityData = clusterConfig.Credentials.CertificateAuthorityData,
                Token = clusterConfig.Credentials.Token,
                ClientCertificateData = clusterConfig.Credentials.ClientCertificateData,
                ClientKeyData = clusterConfig.Credentials.ClientKeyData,
                AuthType = clusterConfig.Credentials.AuthType
            });
        }
    }
}