using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Database.Data;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store.Repositories
{
    public class UserClusterRepository : IUserClusterRepository
    {
        private readonly AppDbContext database;

        public UserClusterRepository(AppDbContext database)
        {
            this.database = database;
        }

        public bool Exists(int userId, string clusterId)
        {
            try
            {
                return database.ClusterConfigurations.Any(c => c.UserId == userId && c.ClusterId == clusterId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error verificando existencia de configuración", ex);
            }
        }

        public ClusterConfiguration? GetClusterConfiguration(int userId, string clusterId)
        {
            try
            {
                return database.ClusterConfigurations
                    .Include(c => c.Credentials)
                    .FirstOrDefault(c => c.UserId == userId && c.ClusterId == clusterId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error obteniendo configuración de cluster", ex);
            }
        }

        public void RemoveClusterConfiguration(int userId, string clusterId)
        {
            try
            {
                var config = database.ClusterConfigurations
                    .FirstOrDefault(c => c.UserId == userId && c.ClusterId == clusterId);

                if (config != null)
                {
                    database.ClusterConfigurations.Remove(config);
                    database.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al eliminar la configuración del cluster (FK o DB)", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error inesperado al eliminar la configuración del cluster", ex);
            }
        }

        public void SaveClusterConfiguration(ClusterConfiguration configuration)
        {
            try
            {
                if (configuration.Id == 0)
                    database.ClusterConfigurations.Add(configuration);
                else
                    database.ClusterConfigurations.Update(configuration);

                database.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al guardar la configuración del cluster (FK o constraint)", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error inesperado al guardar la configuración del cluster", ex);
            }
        }
    }
}
