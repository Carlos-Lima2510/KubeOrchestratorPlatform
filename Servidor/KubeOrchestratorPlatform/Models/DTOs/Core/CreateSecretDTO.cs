using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Core
{
    public class CreateSecretDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Namespace { get; set; } = "default";
        
        public SecretType Type { get; set; }
        
        public Dictionary<string, string> StringData { get; set; } = new();
        
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}