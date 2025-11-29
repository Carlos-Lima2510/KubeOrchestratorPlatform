using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs.ContainerSpecs.ProbeActions
{
    public class HttpHeaderDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}