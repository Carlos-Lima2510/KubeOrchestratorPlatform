using k8s;
using Servidor.KubeOrchestratorPlatform.Factories;
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

        public IKubernetes GetClient(string userId)
        {
            if (!store.Exists(userId))
                throw new KeyNotFoundException($"No se encontraron credenciales para {userId}");

            var credentials = store.Get(userId);
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