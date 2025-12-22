using Servidor.KubeOrchestratorPlatform.Models.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph
{
    public class GraphNodeDTO
    {
        public string Id { get; set; } = string.Empty; 
        public ResourceTypeEnum Type { get; set; } = ResourceTypeEnum.Pod;
        public string Label { get; set; } = string.Empty; 
        public string Status { get; set; } = "Allowed"; 
        public object? Metadata { get; set; }
    }
}