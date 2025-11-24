using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Database.Migrations
{
    public class UserCluster : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ClusterConfiguration> ClusterConfigurations { get; set; } = null!;

        public UserCluster(DbContextOptions<UserCluster> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ClusterConfiguration>()
                .HasOne(c => c.User)
                .WithMany(u => u.Clusters)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClusterConfiguration>()
                .HasIndex(c => new { c.UserId, c.ClusterId, c.Context })
                .IsUnique();
        }
    }
}