namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs.ContainerSpecs.ProbeActions
{
    public class ExecActionDto
    {
        public List<string> Command { get; set; } = new();
    }
}