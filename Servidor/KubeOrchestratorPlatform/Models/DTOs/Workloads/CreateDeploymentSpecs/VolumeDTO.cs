using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs
{
    public class VolumeDTO 
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [RegularExpression("^(EmptyDir|PersistentVolumeClaim|ConfigMap|Secret)$", ErrorMessage = "Tipo de volumen no soportado")]
        public string Type { get; set; } = "EmptyDir"; 
        public string? SourceName { get; set; }
    }
}