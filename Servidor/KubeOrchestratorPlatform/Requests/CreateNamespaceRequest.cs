using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Requests
{
    public class CreateNamespaceRequest
    {
        [Required(ErrorMessage = "El nombre del namespace es obligatorio")]
        [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "El nombre debe ser válido para Kubernetes (minúsculas, números y guiones)")]
        public string Name { get; set; } = string.Empty;
    }
}