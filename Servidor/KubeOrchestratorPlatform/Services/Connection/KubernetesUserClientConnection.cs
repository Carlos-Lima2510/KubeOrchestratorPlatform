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
            var credentials = store.Get(userId, clusterId)
                ?? throw new Exception($"No hay credenciales para el usuario {userId}");
                
            return factory.CreateClient(new ParsedKubeConfig
            {
                Server = credentials.Server,
                CertificateAuthorityData = credentials.CertificateAuthorityData,
                Token = credentials.Token,
                ClientCertificateData = credentials.ClientCertificateData,
                ClientKeyData = credentials.ClientKeyData,
                AuthType = credentials.AuthType
            });
        }
    }
}