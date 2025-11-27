namespace Servidor.KubeOrchestratorPlatform.Responses
{
    public class ClusterConfigurationResponse
    {
        public string ClusterName { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}