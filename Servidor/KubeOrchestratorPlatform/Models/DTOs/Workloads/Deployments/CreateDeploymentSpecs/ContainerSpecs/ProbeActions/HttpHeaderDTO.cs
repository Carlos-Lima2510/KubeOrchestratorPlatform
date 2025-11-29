using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs.ProbeActions
{
    public class HttpHeaderDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}