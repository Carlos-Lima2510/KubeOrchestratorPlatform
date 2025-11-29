using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs;

namespace Servidor.KubeOrchestratorPlatform.Requests
{
    public class CreateDeploymentRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = "default";
        public Dictionary<string, string> Labels { get; set; } = new();
        public Dictionary<string, string> Annotations { get; set; } = new();

        [Range(0, 50)]
        public int Replicas { get; set; } = 1;
        public int MinReadySeconds { get; set; } = 0;
        public DeploymentStrategyDTO Strategy { get; set; } = new();

        [Required]
        [MinLength(1, ErrorMessage = "Debe haber al menos un contenedor")]
        public List<ContainerDto> Containers { get; set; } = new();
        public List<VolumeDTO> Volumes { get; set; } = new();
    }
}