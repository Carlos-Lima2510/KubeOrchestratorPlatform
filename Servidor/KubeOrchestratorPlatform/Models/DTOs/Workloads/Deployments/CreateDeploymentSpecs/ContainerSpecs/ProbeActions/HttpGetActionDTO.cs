namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs.ProbeActions
{
    public class HttpGetActionDTO
    {
        public string Path { get; set; } = "/healthz";
        public int Port { get; set; } = 80;
        public List<HttpHeaderDto> HttpHeaders { get; set; } = new();
    }
}