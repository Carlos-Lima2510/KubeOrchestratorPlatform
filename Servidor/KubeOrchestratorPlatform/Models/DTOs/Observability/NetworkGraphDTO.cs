namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Observability
{
    public class NetworkGraphDTO
    {
        public List<GraphNodeDTO> Nodes { get; set; } = new();
        public List<GraphEdgeDTO> Edges { get; set; } = new();
    }

    public class GraphNodeDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = "unknown";
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class GraphEdgeDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
