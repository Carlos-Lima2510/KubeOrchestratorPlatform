using System.Collections.Concurrent;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Store
{
    public class InMemoryUserClusterStore : IUserClusterStore
    {
        private readonly ConcurrentDictionary<string, UserClusterCredentials> _store;

        public InMemoryUserClusterStore()
        {
            _store = new ConcurrentDictionary<string, UserClusterCredentials>();
        }

        public void Save(string userId, UserClusterCredentials credentials)
        {
            _store[userId] = credentials;
        }

        public UserClusterCredentials Get(string userId)
        {
            if (_store.TryGetValue(userId, out var creds))
                return creds;
            throw new KeyNotFoundException($"No se encontraron credenciales para el usuario {userId}");
        }

        public bool Exists(string userId)
        {
            return _store.ContainsKey(userId);
        }

        public void Remove(string userId)
        {
            _store.TryRemove(userId, out _);
        }
    }
}