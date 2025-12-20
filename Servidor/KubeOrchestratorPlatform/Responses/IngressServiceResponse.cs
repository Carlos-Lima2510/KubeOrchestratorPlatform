namespace Servidor.KubeOrchestratorPlatform.Responses
{
    public class IngressServiceResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
    }
}
