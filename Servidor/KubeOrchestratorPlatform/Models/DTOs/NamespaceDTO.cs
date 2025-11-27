namespace Servidor.KubeOrchestratorPlatform.Models.DTOs
{
    public class NamespaceDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown";
        public DateTime? CreatedAt { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}