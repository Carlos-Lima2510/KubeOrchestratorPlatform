using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments
{
    public class CreateDeploymentDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Namespace { get; set; } = "default";

        public Dictionary<string, string> Labels { get; set; } = new();
        public Dictionary<string, string> Annotations { get; set; } = new();

        [Range(0, 100)]
        public int Replicas { get; set; } = 1;
        public int MinReadySeconds { get; set; } = 0;
        public DeploymentStrategyDTO Strategy { get; set; } = new();
        public List<string> ImagePullSecrets { get; set; } = new();
        public Dictionary<string, string> NodeSelector { get; set; } = new();
        public string ServiceAccountName { get; set; } = "default";


        public List<ContainerDTO> InitContainers { get; set; } = new();

        [Required]
        [MinLength(1, ErrorMessage = "Debe haber al menos un contenedor")]
        public List<ContainerDTO> Containers { get; set; } = new();


        public List<VolumeDTO> Volumes { get; set; } = new();
    }
}