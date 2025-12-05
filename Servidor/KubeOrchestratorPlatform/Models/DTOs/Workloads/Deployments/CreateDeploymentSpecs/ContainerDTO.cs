using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs
{
    public class ContainerDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Image { get; set; } = string.Empty;
        public string ImagePullPolicy { get; set; } = "IfNotPresent";
        public List<string> Command { get; set; } = new();
        public List<string> Args { get; set; } = new();
        public List<ContainerPortDTO> Ports { get; set; } = new();
        public Dictionary<string, string> Environment { get; set; } = new();
        public List<EnvFromSourceDTO> EnvFrom { get; set; } = new();
        public ResourceRequirementsDTO Resources { get; set; } = new();


        public ProbeDTO? LivenessProbe { get; set; }
        public ProbeDTO? ReadinessProbe { get; set; }
        public ProbeDTO? StartupProbe { get; set; }


        public SecurityContextDTO? SecurityContext { get; set; }

        public List<VolumeMountDTO> VolumeMounts { get; set; } = new();
    }
}