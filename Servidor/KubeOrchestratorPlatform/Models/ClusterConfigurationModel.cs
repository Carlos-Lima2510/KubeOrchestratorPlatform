namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class ClusterConfiguration
    {
        public int Id { get; set; }                     
        public int UserId { get; set; }                
        public required string ClusterId { get; set; }  
        public string? Context { get; set; }          
        public string DisplayName { get; set; } = "";  
        public string ServerUrl { get; set; } = null!;
        public int CredentialsId { get; set; }
        public required UserClusterCredentials Credentials { get; set; }
        public string EncryptionVersion { get; set; } = "v1";
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }

        public User User { get; set; } = null!;      
    }
}
