namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph
{
    public class GraphEdgeDTO
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string Status { get; set; } = "Allowed";
        public object? Metadata { get; set; }
    }
}