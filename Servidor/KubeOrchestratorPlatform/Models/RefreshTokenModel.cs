namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public required string TokenHash { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
    }
}
