using StackExchange.Redis;
using System.Text.Json;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store
{
    public class RedisUserClusterStore : IUserClusterStore
    {
        private readonly IDatabase database;
        private const string Prefix = "kubecreds:";

        public RedisUserClusterStore(IConnectionMultiplexer redis)
        {
            database = redis.GetDatabase();
        }

        public void Save(string userId, UserClusterCredentials creds)
        {
            var json = JsonSerializer.Serialize(creds);

            database.StringSet($"{Prefix}{userId}", json, 
                TimeSpan.FromHours(12));
        }

        public UserClusterCredentials? Get(string userId)
        {
            var value = database.StringGet($"{Prefix}{userId}");

            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<UserClusterCredentials>(value!);
        }

        public bool Exists(string userId)
            => database.KeyExists($"{Prefix}{userId}");

        public void Remove(string userId)
            => database.KeyDelete($"{Prefix}{userId}");
    }
}