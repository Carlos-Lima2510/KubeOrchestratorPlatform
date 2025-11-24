using System.Text.Json;
using StackExchange.Redis;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store.Cache
{
    public class UserClusterCache : IUserClusterCache
    {
        private readonly IDatabase database;
        private readonly string prefix = "usercluster:";

        public UserClusterCache(IConnectionMultiplexer redis)
        {
            database = redis.GetDatabase();
        }
        private string Key(int userId, string clusterId) => $"{prefix}{userId}:{clusterId}";
        public bool Exists(int userId, string clusterId)
        {
            return database.KeyExists(Key(userId, clusterId));
        }

        public UserClusterCredentials? Get(int userId, string clusterId)
        {
            try
            {
                var value = database.StringGet(Key(userId, clusterId));
                if (value.IsNullOrEmpty) return null;
                return JsonSerializer.Deserialize<UserClusterCredentials>(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener cache Redis: {ex.Message}");
                return null;
            }
        }

        public void Set(int userId, string clusterId, UserClusterCredentials credentials)
        {
            try
            {
                var json = JsonSerializer.Serialize(credentials);
                database.StringSet(Key(userId, clusterId), json, TimeSpan.FromHours(12));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar cache Redis: {ex.Message}");
            }
        }

        public void Remove(int userId, string clusterId)
        {
            try
            {
                database.KeyDelete(Key(userId, clusterId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar cache Redis: {ex.Message}");
            }
        }

    }
}