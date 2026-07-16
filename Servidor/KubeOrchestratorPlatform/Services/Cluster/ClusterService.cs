using Microsoft.VisualBasic;
using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Cache;
using Servidor.KubeOrchestratorPlatform.Repositories;
using Servidor.KubeOrchestratorPlatform.Services.Security;

namespace Servidor.KubeOrchestratorPlatform.Services.Cluster
{
    public class ClusterService : IClusterService
    {
        private readonly IUserClusterRepository repository;
        private readonly IUserClusterCache cache;
        private readonly IEncryptionService encryptionService;

        public ClusterService(IUserClusterRepository repository, IUserClusterCache cache, IEncryptionService encryptionService)
        {
            this.repository = repository;
            this.cache = cache;
            this.encryptionService = encryptionService;
        }
        public ConnectedCluster? GetCluster(int userId, string clusterId)
        {
            var cached = cache.Get(userId, clusterId);
            if (cached != null)
            {
                return cached;
            }

            var dbConfig = repository.GetClusterConfiguration(userId, clusterId);

            if (dbConfig != null)
            {
                DecryptCredentials(dbConfig.Credentials, dbConfig.EncryptionVersion);
                cache.Set(userId, clusterId, dbConfig);
            }

            return dbConfig;
        }
        private void DecryptCredentials(ClusterCredentials credentials, string version)
        {
            if (credentials == null) return;
            credentials.Token = encryptionService.Decrypt(credentials.Token!, version);
            credentials.ClientCertificateData = encryptionService.Decrypt(credentials.ClientCertificateData!, version);
            credentials.ClientKeyData = encryptionService.Decrypt(credentials.ClientKeyData!, version);
            credentials.CertificateAuthorityData = encryptionService.Decrypt(credentials.CertificateAuthorityData!, version);
        }
        public void Save(int userId, string publicClusterId, ClusterCredentials credentials, string serverUrl, string displayName)
        {
            var config = GetOrCreateCluster(userId, publicClusterId);

            UpdateClusterMetadata(config, serverUrl, displayName, encryptionService.CurrentVersion);

            UpdateClusterCredentials(config, credentials);

            var rawCredentials = SnapshotCredentials(config);

            EncryptCredentialsInPlace(config);
            repository.SaveClusterConfiguration(config);

            EnsureUserNavigationProperty(config);

            RestoreCredentialsInPlace(config, rawCredentials);
            cache.Set(userId, publicClusterId, config);
        }

        private void RestoreCredentialsInPlace(ConnectedCluster config, (string? Token, string? Cert, string? Key, string? CA) rawCredentials)
        {
            config.Credentials.Token = rawCredentials.Token;
            config.Credentials.ClientCertificateData = rawCredentials.Cert;
            config.Credentials.ClientKeyData = rawCredentials.Key;
            config.Credentials.CertificateAuthorityData = rawCredentials.CA;
        }

        private void EnsureUserNavigationProperty(ConnectedCluster config)
        {
            if (config.User == null)
            {
                var reloaded = repository.GetClusterConfiguration(config.UserId, config.PublicClusterId);
                if (reloaded?.User != null)
                {
                    config.User = reloaded.User;
                }
            }
        }

        private void EncryptCredentialsInPlace(ConnectedCluster config)
        {
            EncryptCredentials(config); 
        }

        private (string? Token, string? Cert, string? Key, string? CA) SnapshotCredentials(ConnectedCluster config)
        {
            return (
                config.Credentials.Token,
                config.Credentials.ClientCertificateData,
                config.Credentials.ClientKeyData,
                config.Credentials.CertificateAuthorityData
            );
        }
        private void UpdateClusterCredentials(ConnectedCluster config, ClusterCredentials credentials)
        {
            config.Credentials ??= new ClusterCredentials();

            config.Credentials.Token = credentials.Token;
            config.Credentials.ClientCertificateData = credentials.ClientCertificateData;
            config.Credentials.ClientKeyData = credentials.ClientKeyData;
            config.Credentials.CertificateAuthorityData = credentials.CertificateAuthorityData;
            config.Credentials.AuthType = credentials.AuthType;
        }

        private void UpdateClusterMetadata(ConnectedCluster config, string serverUrl, string displayName, string currentVersion)
        {
            config.ServerUrl = serverUrl;
            config.DisplayName = displayName;
            config.EncryptionVersion = currentVersion;
        }

        private ConnectedCluster GetOrCreateCluster(int userId, string publicClusterId)
        {
            var config = repository.GetClusterConfiguration(userId, publicClusterId);

            if (config == null)
            {
                return new ConnectedCluster
                {
                    UserId = userId,
                    PublicClusterId = publicClusterId,
                    IsActive = true,
                    Credentials = new ClusterCredentials()
                };
            }

            return config;
        }

        private (string? Token, string? ClientCert, string? ClientKey, string? CAData) EncryptCredentials(ConnectedCluster config)
        {
            var rawToken = config.Credentials.Token;
            var rawCert = config.Credentials.ClientCertificateData;
            var rawKey = config.Credentials.ClientKeyData;
            var rawCA = config.Credentials.CertificateAuthorityData;

            config.Credentials.Token = encryptionService.Encrypt(rawToken!);
            config.Credentials.ClientCertificateData = encryptionService.Encrypt(rawCert!);
            config.Credentials.ClientKeyData = encryptionService.Encrypt(rawKey!);
            config.Credentials.CertificateAuthorityData = encryptionService.Encrypt(rawCA!);

            return (rawToken, rawCert, rawKey, rawCA);
        }
        public void Remove(int userId, string clusterId)
        {
            repository.RemoveClusterConfiguration(userId, clusterId);
            cache.Remove(userId, clusterId);
        }
        public bool Exists(int userId, string clusterId)
        {
            if (cache.Exists(userId, clusterId)) return true;
            return repository.Exists(userId, clusterId);
        }
        public (string UserName, string ClusterName) GetClusterContext(int userId, string clusterId)
        {
            var cached = cache.Get(userId, clusterId);
            if (cached != null)
            {
                return (cached.User?.Username ?? "Unknown", cached.DisplayName ?? clusterId);
            }

            var dbNames = repository.GetClusterNames(userId, clusterId);

            return (dbNames.UserName ?? "Unknown", dbNames.ClusterName ?? clusterId);
        }
    }
}