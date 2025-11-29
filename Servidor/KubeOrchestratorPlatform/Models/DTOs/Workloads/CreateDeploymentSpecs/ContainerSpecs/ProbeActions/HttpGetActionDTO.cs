namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs.ContainerSpecs.ProbeActions
{
    public class HttpGetActionDto
    {
        public string Path { get; set; } = "/healthz";
        public int Port { get; set; } = 80;
        public List<HttpHeaderDto> HttpHeaders { get; set; } = new();
    }
}