namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads
{
    public class ResourceRequirementsDTO
    {
        public Dictionary<string, string> Requests { get; set; } = new();
        public Dictionary<string, string> Limits { get; set; } = new();
    }
}