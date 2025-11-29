namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs.ContainerSpecs
{
    public class SecurityContextDTO
    {
        public bool? Privileged { get; set; }
        public bool? ReadOnlyRootFilesystem { get; set; }
        public int? RunAsUser { get; set; }
        public int? RunAsGroup { get; set; }
    }
}