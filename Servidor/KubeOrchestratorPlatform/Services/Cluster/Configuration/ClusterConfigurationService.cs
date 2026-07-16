using k8s;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Responses;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Parser;
using Servidor.KubeOrchestratorPlatform.Services.Security;

namespace Servidor.KubeOrchestratorPlatform.Services.Cluster.Configuration
{
    public class ClusterConfigurationService : IClusterConfigurationService
    {
        private readonly IClusterService clusterService;
        private readonly IKubeConfigParser parserService;
        private readonly IAuditService auditService;
        private readonly IKubernetesClientFactory clientFactory;
        private readonly INetworkSafetyValidator networkSafetyValidator;

        public ClusterConfigurationService(
            IClusterService clusterService,
            IKubeConfigParser parserService,
            IAuditService auditService,
            IKubernetesClientFactory clientFactory,
            INetworkSafetyValidator networkSafetyValidator)
        {
            this.clusterService = clusterService;
            this.parserService = parserService;
            this.auditService = auditService;
            this.clientFactory = clientFactory;
            this.networkSafetyValidator = networkSafetyValidator;
        }
        public Task DeleteConfigurationAsync(int userId, string clusterId)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);

            if (clusterName == clusterId && !clusterService.Exists(userId, clusterId))
            {
                throw new KeyNotFoundException("La configuración no existe.");
            }

            try
            {
                clusterService.Remove(userId, clusterId);

                auditService.LogOperation(userName, clusterName, "DeleteClusterConfig", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, "DeleteClusterConfig", false, ex.Message);
                throw;
            }

            return Task.CompletedTask;
        }
        public async Task<ClusterConfigurationResponse> UploadKubeConfigAsync(int userId, string clusterId, Stream kubeConfigStream)
        {
            try
            {
                using var reader = new StreamReader(kubeConfigStream);
                var yamlContent = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(yamlContent))
                    throw new ArgumentException("El archivo de configuración está vacío.");

                var parsed = parserService.Parse(yamlContent);

                await networkSafetyValidator.EnsureSafeClusterEndpointAsync(parsed.Server);

                try
                {
                    var tempClient = clientFactory.CreateClient(parsed);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await tempClient.CoreV1.ListNamespaceAsync(limit: 1, cancellationToken: cts.Token);
                }
                catch (Exception ex)
                {
                    throw new Exception($"El archivo es válido, pero no se pudo conectar al cluster: {ex.Message}");
                }

                var creds = new ClusterCredentials
                {
                    CertificateAuthorityData = parsed.CertificateAuthorityData,
                    Token = parsed.Token,
                    ClientCertificateData = parsed.ClientCertificateData,
                    ClientKeyData = parsed.ClientKeyData,
                    AuthType = parsed.AuthType
                };

                var displayName = parsed.ClusterName ?? clusterId;
                var k8sUser = parsed.UserName ?? "UnknownK8sUser";

                clusterService.Save(userId, clusterId, creds, parsed.Server, displayName);

                auditService.LogOperation(k8sUser, displayName, "UploadKubeConfig", true);

                return new ClusterConfigurationResponse
                {
                    ClusterName = displayName,
                    ServerUrl = parsed.Server,
                    Message = "Configuración cargada correctamente."
                };
            }
            catch (Exception ex)
            {
                auditService.LogOperation($"User:{userId}", clusterId, "UploadKubeConfig", false, ex.Message);
                throw; 
            }
        }
    }
}