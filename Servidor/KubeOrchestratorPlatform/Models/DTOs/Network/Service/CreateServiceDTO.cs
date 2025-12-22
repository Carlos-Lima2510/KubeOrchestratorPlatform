using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.Enums;

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

        public ServiceType Type { get; set; } = ServiceType.ClusterIP;

        [Required]
        [MinLength(1, ErrorMessage = "El servicio debe exponer al menos un puerto")]
        public List<ServicePortDto> Ports { get; set; } = new();

        public SessionAffinity SessionAffinity { get; set; } = SessionAffinity.None;

        public TrafficPolicy InternalTrafficPolicy { get; set; } = TrafficPolicy.Cluster;

        public TrafficPolicy ExternalTrafficPolicy { get; set; } = TrafficPolicy.Cluster;

        public IpFamilyPolicy IpFamilyPolicy { get; set; } = IpFamilyPolicy.SingleStack;
    }
}