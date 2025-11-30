using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Network
{
    public class ServicePortDto
    {
        [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "El nombre del puerto debe ser válido")]
        public string? Name { get; set; }

        [Required]
        [Range(1, 65535)]
        public int Port { get; set; }

        [Range(1, 65535)]
        public int? TargetPort { get; set; }

        [Range(30000, 32767)]
        public int? NodePort { get; set; }
        
        [RegularExpression("^(TCP|UDP|SCTP)$")]
        public string Protocol { get; set; } = "TCP";
    }
}