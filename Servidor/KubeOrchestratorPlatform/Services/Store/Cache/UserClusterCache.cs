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

        public ClusterCredentials? Get(int userId, string clusterId)
        {
            try
            {
                var value = database.StringGet(Key(userId, clusterId));
                if (value.IsNullOrEmpty) return null;
                
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                    PropertyNameCaseInsensitive = true
                };
                
                // Deserializar el DTO y reconstruir ClusterCredentials
                using var doc = JsonDocument.Parse(value!.ToString());
                var root = doc.RootElement;
                
                return new ClusterCredentials
                {
                    Id = root.TryGetProperty("Id", out var id) ? id.GetInt32() : 0,
                    CertificateAuthorityData = root.TryGetProperty("CertificateAuthorityData", out var ca) ? ca.GetString() : null,
                    Token = root.TryGetProperty("Token", out var token) ? token.GetString() : null,
                    ClientCertificateData = root.TryGetProperty("ClientCertificateData", out var cert) ? cert.GetString() : null,
                    ClientKeyData = root.TryGetProperty("ClientKeyData", out var key) ? key.GetString() : null,
                    AuthType = root.TryGetProperty("AuthType", out var auth) ? auth.GetString() : null,
                    ConnectedClusterId = root.TryGetProperty("ConnectedClusterId", out var clusterIdProp) ? clusterIdProp.GetInt32() : 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener cache Redis: {ex.Message}");
                return null;
            }
        }

        public void Set(int userId, string clusterId, ClusterCredentials credentials)
        {
            try
            {
                // Crear un DTO sin referencias circulares para el cache
                var cacheDto = new
                {
                    credentials.Id,
                    credentials.CertificateAuthorityData,
                    credentials.Token,
                    credentials.ClientCertificateData,
                    credentials.ClientKeyData,
                    credentials.AuthType,
                    credentials.ConnectedClusterId
                };
                
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                
                var json = JsonSerializer.Serialize(cacheDto, options);
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