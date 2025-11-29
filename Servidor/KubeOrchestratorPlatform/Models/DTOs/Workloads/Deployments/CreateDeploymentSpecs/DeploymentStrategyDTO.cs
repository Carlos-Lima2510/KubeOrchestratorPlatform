using System.ComponentModel.DataAnnotations;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs
{
    public class DeploymentStrategyDTO
    {
        [RegularExpression("^(RollingUpdate|Recreate)$", ErrorMessage = "El tipo de estrategia debe ser 'RollingUpdate' o 'Recreate'")]
        public string Type { get; set; } = "RollingUpdate"; 
        public RollingUpdateConfigDto? RollingUpdate { get; set; }
    }

    public class RollingUpdateConfigDto
    {
        public string MaxUnavailable { get; set; } = "25%";
        public string MaxSurge { get; set; } = "25%";
    }
}