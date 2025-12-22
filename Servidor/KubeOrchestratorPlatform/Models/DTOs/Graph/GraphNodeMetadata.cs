namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph
{
    public class GraphNodeMetadata
    {
        public string? Issuer { get; set; }
        public DateTime? ExpirationDate { get; set; }
        
        public string? Ip { get; set; }
        public List<string>? Ports { get; set; }
        public string? NodeName { get; set; }
        public string? Cpu { get; set; }
        public string? Mem { get; set; }
    }
}