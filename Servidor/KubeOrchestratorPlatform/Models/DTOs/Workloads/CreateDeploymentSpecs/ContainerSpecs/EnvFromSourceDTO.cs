namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs.ContainerSpecs
{
    public class EnvFromSourceDTO
    {
        public string? ConfigMapRef { get; set; }
        public string? SecretRef { get; set; }
    }
}