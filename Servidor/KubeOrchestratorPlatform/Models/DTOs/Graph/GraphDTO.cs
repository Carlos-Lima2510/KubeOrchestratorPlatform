namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph
{
    public class GraphDTO
    {
        public List<GraphNodeDTO> Nodes { get; set; } = new();
        public List<GraphEdgeDTO> Edges { get; set; } = new();
    }
}