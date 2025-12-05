using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs
{
    public class NamespaceDTO
    {
        [Required(ErrorMessage = "El nombre del namespace es obligatorio")]
        [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "El nombre debe ser válido para Kubernetes (minúsculas, números y guiones)")]
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown";
        public DateTime? CreatedAt { get; set; }
        public Dictionary<string, string> Labels { get; set; } = [];
    }
}