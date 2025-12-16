using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs
{
    public class VolumeDTO 
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public VolumeType Type { get; set; } 
        public string? SourceName { get; set; }
    }
}