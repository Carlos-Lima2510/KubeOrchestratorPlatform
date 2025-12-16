using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs
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
        public Protocol Protocol { get; set; }
    }
}