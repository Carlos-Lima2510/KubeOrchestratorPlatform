using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<ClusterConfiguration> ClusterConfigurations => Set<ClusterConfiguration>();
        public DbSet<UserClusterCredentials> UserClusterCredentials => Set<UserClusterCredentials>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RELACIÓN 1:N => User -> ClusterConfigurations
            modelBuilder.Entity<User>()
                .HasMany(u => u.Clusters)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // RELACIÓN 1:1 => ClusterConfiguration -> Credentials
            modelBuilder.Entity<ClusterConfiguration>()
                .HasOne(c => c.Credentials)
                .WithOne()
                .HasForeignKey<ClusterConfiguration>(c => c.CredentialsId);
        }
    }
}
