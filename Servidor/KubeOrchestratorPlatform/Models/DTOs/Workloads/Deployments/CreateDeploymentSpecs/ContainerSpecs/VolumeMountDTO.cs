using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs
{
    public class VolumeMountDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string MountPath { get; set; } = string.Empty;
        public bool ReadOnly { get; set; } = false;
        public string? SubPath { get; set; }
    }
}