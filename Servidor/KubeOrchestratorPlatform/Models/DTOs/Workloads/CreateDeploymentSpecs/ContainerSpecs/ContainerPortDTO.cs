using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.CreateDeploymentSpecs.ContainerSpecs
{
    public class ContainerPortDTO
    {
        [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "El nombre del puerto debe ser válido (minúsculas y guiones)")]
        [MaxLength(15, ErrorMessage = "El nombre del puerto no puede tener más de 15 caracteres")]
        public string? Name { get; set; }

        [Required]
        [Range(1, 65535)]
        public int ContainerPort { get; set; }

        [Range(1, 65535)]
        public int? HostPort { get; set; }

        public string? HostIP { get; set; }

        [RegularExpression("^(TCP|UDP|SCTP)$", ErrorMessage = "El protocolo debe ser TCP, UDP o SCTP")]
        public string Protocol { get; set; } = "TCP";
    }
}