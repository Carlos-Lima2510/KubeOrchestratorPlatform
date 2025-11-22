using k8s;
using Microsoft.Extensions.Options;

namespace Servidor.KubeOrchestratorPlatform.Kubernetes
{
    public class KubeConnectionService : IKubeConnection
    {
        private readonly ClusterConfig clusterConfig;
        private IKubernetes? client;
        private readonly ILogger<KubeConnectionService> logger;

        public string Namespace => clusterConfig.Namespace ?? "default";

        public KubeConnectionService(IOptions<ClusterConfig> config, ILogger<KubeConnectionService> logger)
        {
            this.clusterConfig = config.Value;
            this.logger = logger;
        }

        public IKubernetes GetClient()
        {
            if (client != null)
                return client;

            KubernetesClientConfiguration kubeConfig;

            try
            {
                if (clusterConfig.UseInCluster)
                {
                    logger.LogInformation("Using in-cluster Kubernetes configuration.");
                    kubeConfig = KubernetesClientConfiguration.InClusterConfig();
                }
                else if (!string.IsNullOrWhiteSpace(clusterConfig.KubeConfigPath))
                {
                    logger.LogInformation("Using kubeconfig path: {path}", clusterConfig.KubeConfigPath);
                    kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(clusterConfig.KubeConfigPath);
                }
                else if (!string.IsNullOrWhiteSpace(clusterConfig.ApiServerUrl) &&
                         !string.IsNullOrWhiteSpace(clusterConfig.Token))
                {
                    logger.LogInformation("Using direct API server connection.");
                    kubeConfig = new KubernetesClientConfiguration
                    {
                        Host = clusterConfig.ApiServerUrl,
                        AccessToken = clusterConfig.Token,
                        SkipTlsVerify = true
                    };
                }
                else
                {
                    throw new Exception("No valid Kubernetes connection configuration found.");
                }

                client = new k8s.Kubernetes(kubeConfig);
                logger.LogInformation("Kubernetes client instantiated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Kubernetes client");
                throw;
            }

            return client;
        }
    }
}
