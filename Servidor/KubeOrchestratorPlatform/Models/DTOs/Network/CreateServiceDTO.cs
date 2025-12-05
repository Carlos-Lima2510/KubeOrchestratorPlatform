using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Network
{
    public class CreateServiceDTO
    {
        [Required(ErrorMessage = "El nombre del servicio es obligatorio")]
        [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "El nombre debe ser válido para Kubernetes (DNS-1123)")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Namespace { get; set; } = "default";
        public Dictionary<string, string> Labels { get; set; } = new();
        public Dictionary<string, string> Annotations { get; set; } = new();

        [Required(ErrorMessage = "Debes especificar un selector para conectar con los pods")]
        public Dictionary<string, string> Selector { get; set; } = new();

        [RegularExpression("^(ClusterIP|NodePort|LoadBalancer|ExternalName)$", ErrorMessage = "Tipo de servicio no válido")]
        public string Type { get; set; } = "ClusterIP";

        [Required]
        [MinLength(1, ErrorMessage = "El servicio debe exponer al menos un puerto")]
        public List<ServicePortDto> Ports { get; set; } = new();

        [RegularExpression("^(None|ClientIP)$")]
        public string SessionAffinity { get; set; } = "None";

        [RegularExpression("^(Cluster|Local)$")]
        public string InternalTrafficPolicy { get; set; } = "Cluster";

        [RegularExpression("^(Cluster|Local)$")]
        public string ExternalTrafficPolicy { get; set; } = "Cluster";

        [RegularExpression("^(SingleStack|PreferDualStack|RequireDualStack)$")]
        public string IpFamilyPolicy { get; set; } = "SingleStack";
    }
}