namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class ConnectedCluster
    {
        public int Id { get; set; }

        public int UserId { get; set; }                
        public User User { get; set; } = null!;
        
        public required string PublicClusterId { get; set; } 
        public string? Context { get; set; }          
        public string DisplayName { get; set; } = "";  
        public string ServerUrl { get; set; } = null!;

        public bool IsActive { get; set; } = false;
        public string EncryptionVersion { get; set; } = "v1";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }

        public required ClusterCredentials Credentials { get; set; }
    }
}
