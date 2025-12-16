using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs
{
    public class DeploymentStrategyDTO
    {
        public StrategyType Type { get; set; }
        public RollingUpdateConfigDto? RollingUpdate { get; set; }
    }

    public class RollingUpdateConfigDto
    {
        public string MaxUnavailable { get; set; } = "25%";
        public string MaxSurge { get; set; } = "25%";
    }
}