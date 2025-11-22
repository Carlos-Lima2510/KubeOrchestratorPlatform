namespace Servidor.KubeOrchestratorPlatform.Kubernetes
{
    public class ClusterConfig
    {
        public string? KubeConfigPath { get; set; }
        public string? Namespace { get; set; } = "default";
        public string? ApiServerUrl { get; set; }
        public string? Token { get; set; }
        public bool UseInCluster { get; set; } = false;
    }
}
