using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Core
{
    public class CreateConfigMapDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Namespace { get; set; } = "default";
        
        public Dictionary<string, string> Data { get; set; } = new();
        
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}