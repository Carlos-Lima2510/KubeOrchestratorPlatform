using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Database.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ClusterConfiguration> ClusterConfigurations { get; set; } = null!;
        public DbSet<UserClusterCredentials> UserClusterCredentials { get; set; } = null!;

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

            // ClusterConfigurations
            modelBuilder.Entity<ClusterConfiguration>()
                .HasOne(c => c.User)
                .WithMany(u => u.Clusters)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClusterConfiguration>()
                .HasOne(c => c.Credentials)
                .WithMany()
                .HasForeignKey(c => c.CredentialsId);

            modelBuilder.Entity<ClusterConfiguration>()
                .HasIndex(c => new { c.UserId, c.ClusterId, c.Context })
                .IsUnique();
        }
    }
}
