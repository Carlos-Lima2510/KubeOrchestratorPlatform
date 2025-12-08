namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Pods
{
    public class PodDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public string? Message { get; set; } 
        public int Restarts { get; set; }
        public string Node { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public List<string> Containers { get; set; } = new();
    }
}