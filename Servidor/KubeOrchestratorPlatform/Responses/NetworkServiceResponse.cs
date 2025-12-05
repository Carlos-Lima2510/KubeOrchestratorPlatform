namespace Servidor.KubeOrchestratorPlatform.Responses
{
    public class NetworkServiceResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Warning { get; set; }
    }
}