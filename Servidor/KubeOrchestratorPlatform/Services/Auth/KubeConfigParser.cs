using k8s;
using k8s.KubeConfigModels;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Auth
{
    public class KubeConfigParser : IKubeConfigParser
    {
        public ParsedKubeConfig Parse(string kubeConfigYaml)
        {
            var config = KubernetesYaml.Deserialize<K8SConfiguration>(kubeConfigYaml);

            if (string.IsNullOrEmpty(config?.CurrentContext))
                throw new Exception("El kubeconfig no especifica 'current-context'.");

            var context = config.Contexts
                .FirstOrDefault(c => c.Name == config.CurrentContext)
                ?? throw new Exception("El contexto especificado en 'current-context' no existe.");

            var cluster = config.Clusters
                .FirstOrDefault(c => c.Name == context.ContextDetails.Cluster)
                ?? throw new Exception("El cluster referenciado por el contexto no existe.");

            var user = config.Users
                .FirstOrDefault(u => u.Name == context.ContextDetails.User)
                ?? throw new Exception("El usuario referenciado por el contexto no existe.");

            var clusterInfo = cluster.ClusterEndpoint;
            var userInfo = user.UserCredentials;

            return new ParsedKubeConfig
            {
                Server = clusterInfo.Server,
                CertificateAuthorityData = clusterInfo.CertificateAuthorityData,

                Token = userInfo.Token,

                ClientCertificateData = userInfo.ClientCertificateData,
                ClientKeyData = userInfo.ClientKeyData,

                AuthType = DetectAuthType(userInfo)
            };
        }

        private string DetectAuthType(UserCredentials userInfo)
        {
            var user = userInfo;
            
            if (!string.IsNullOrEmpty(user.Token))
                return "Token";

            if (!string.IsNullOrEmpty(user.ClientCertificateData) &&
                !string.IsNullOrEmpty(user.ClientKeyData))
                return "ClientCertificate";
            
            return "Unknown";
        }
    }
}