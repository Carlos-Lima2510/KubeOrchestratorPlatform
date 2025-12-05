using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs.ProbeActions;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs
{
    public class ProbeDTO
    {
        public HttpGetActionDTO? HttpGet { get; set; }
        public TcpSocketActionDto? TcpSocket { get; set; }
        public ExecActionDto? Exec { get; set; }

        public int InitialDelaySeconds { get; set; } = 0;
        public int TimeoutSeconds { get; set; } = 1;
        public int PeriodSeconds { get; set; } = 10;
        public int SuccessThreshold { get; set; } = 1;
        public int FailureThreshold { get; set; } = 3;
    }
}