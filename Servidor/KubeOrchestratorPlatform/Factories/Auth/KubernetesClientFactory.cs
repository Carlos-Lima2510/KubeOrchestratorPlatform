using k8s;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Factories
{
    public class KubernetesClientFactory : IKubernetesClientFactory
    {
        public IKubernetes CreateClient(ParsedKubeConfig config)
        {
            var k8sConfig = new KubernetesClientConfiguration
            {
                Host = config.Server,
                SslCaCerts = string.IsNullOrEmpty(config.CertificateAuthorityData)
                    ? null
                    : new System.Security.Cryptography.X509Certificates.X509Certificate2Collection(
                        new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            Convert.FromBase64String(config.CertificateAuthorityData)))
            };

            if (config.AuthType == "Token")
            {
                k8sConfig.AccessToken = config.Token;
            }
            else if (config.AuthType == "ClientCertificate")
            {
                k8sConfig.ClientCertificateData = config.ClientCertificateData;
                k8sConfig.ClientCertificateKeyData = config.ClientKeyData;
            }
            else
            {
                throw new Exception($"AuthType {config.AuthType} no soportado todavía");
            }

            return new k8s.Kubernetes(k8sConfig);
        }
    }
}