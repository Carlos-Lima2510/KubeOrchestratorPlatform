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
                return database.ConnectedClusters.Any(c => c.UserId == userId && c.PublicClusterId == clusterId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error verificando existencia de configuración", ex);
            }
        }

        public ConnectedCluster? GetClusterConfiguration(int userId, string clusterId)
        {
            try
            {
                return database.ConnectedClusters
                    .Include(c => c.Credentials)
                    .Include(c => c.User)
                    .FirstOrDefault(c => c.UserId == userId && c.PublicClusterId == clusterId);
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
                var config = database.ConnectedClusters
                    .FirstOrDefault(c => c.UserId == userId && c.PublicClusterId == clusterId);

                if (config != null)
                {
                    database.ConnectedClusters.Remove(config);
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

        public void SaveClusterConfiguration(ConnectedCluster configuration)
        {
            try
            {
                if (configuration.Id == 0)
                    database.ConnectedClusters.Add(configuration);
                else
                    database.ConnectedClusters.Update(configuration);

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
