using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Database.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ConnectedCluster> ConnectedClusters { get; set; } = null!;
        public DbSet<ClusterCredentials> ClusterCredentials { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Relation: User -> ConnectedClusters (1:N)
            modelBuilder.Entity<ConnectedCluster>()
                .HasOne(c => c.User)
                .WithMany(u => u.Clusters)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation: ConnectedCluster <-> ClusterCredentials (1:1)
            modelBuilder.Entity<ConnectedCluster>()
                .HasOne(c => c.Credentials)
                .WithOne(cc => cc.ConnectedCluster)
                .HasForeignKey<ClusterCredentials>(cc => cc.ConnectedClusterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: User + ClusterId + Context
            modelBuilder.Entity<ConnectedCluster>()
                .HasIndex(c => new { c.UserId, c.ClusterId, c.Context })
                .IsUnique();
        }

    }
}
