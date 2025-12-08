namespace Servidor.KubeOrchestratorPlatform.Models.DTOs
{
    public class ClusterMetricsDTO
    {
        public string Timestamp { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }
}