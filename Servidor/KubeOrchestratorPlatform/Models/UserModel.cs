namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public ICollection<ClusterConfiguration> Clusters { get; set; } = new List<ClusterConfiguration>();
    }
}