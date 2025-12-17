using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Core
{
    public class CreateSecretDTO
    {
        [Required]
        
        [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", 
            ErrorMessage = "El nombre debe ser minúsculas, números o guiones y no puede empezar/terminar con guión.")]
        [StringLength(63, ErrorMessage = "El nombre no puede exceder los 63 caracteres.")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Namespace { get; set; } = "default";
        
        public SecretType Type { get; set; }
        
        public Dictionary<string, string> StringData { get; set; } = new();
        
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}