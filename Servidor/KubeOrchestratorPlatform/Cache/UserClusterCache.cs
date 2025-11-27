using System.Text.Json;
using StackExchange.Redis;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Cache
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
        public ConnectedCluster? Get(int userId, string clusterId)
        {
            try
            {
                var json = database.StringGet(Key(userId, clusterId));
                if (json.IsNullOrEmpty) return null;

                var cacheModel = JsonSerializer.Deserialize<CachedCluster>(json!);
                
                if (cacheModel == null) return null;

                return new ConnectedCluster
                {
                    UserId = userId,
                    PublicClusterId = clusterId,
                    
                    ServerUrl = cacheModel.ServerUrl,     
                    DisplayName = cacheModel.DisplayName,
                    
                    User = new User 
                    { 
                        Username = cacheModel.Username,
                        Email = "",
                        PasswordHash = ""
                    },

                    Credentials = new ClusterCredentials
                    {
                        Id = cacheModel.CredentialId,
                        Token = cacheModel.Token,
                        CertificateAuthorityData = cacheModel.CertificateAuthorityData,
                        ClientCertificateData = cacheModel.ClientCertificateData,
                        ClientKeyData = cacheModel.ClientKeyData,
                        AuthType = cacheModel.AuthType
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Redis Get: {ex.Message}");
                return null;
            }
        }
        public void Set(int userId, string clusterId, ConnectedCluster cluster)
        {
            try
            {
                var cacheModel = new CachedCluster
                {
                    ServerUrl = cluster.ServerUrl,
                    DisplayName = cluster.DisplayName,
                    Username = cluster.User?.Username ?? "Unknown", 

                    CredentialId = cluster.Credentials?.Id ?? 0,
                    Token = cluster.Credentials?.Token,
                    CertificateAuthorityData = cluster.Credentials?.CertificateAuthorityData,
                    ClientCertificateData = cluster.Credentials?.ClientCertificateData,
                    ClientKeyData = cluster.Credentials?.ClientKeyData,
                    AuthType = cluster.Credentials?.AuthType ?? "Token"
                };
                
                var json = JsonSerializer.Serialize(cacheModel);
                
                database.StringSet(Key(userId, clusterId), json, TimeSpan.FromHours(12));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Redis Set: {ex.Message}");
            }
        }

        public void Remove(int userId, string clusterId)
        {
            database.KeyDelete(Key(userId, clusterId));
        }
    }
}