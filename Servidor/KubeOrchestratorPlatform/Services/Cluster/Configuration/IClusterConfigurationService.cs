using Servidor.KubeOrchestratorPlatform.Responses;

namespace Servidor.KubeOrchestratorPlatform.Services.Cluster.Configuration
{
    public interface IClusterConfigurationService
    {
        Task<ClusterConfigurationResponse> UploadKubeConfigAsync(int userId, string clusterId, Stream kubeConfigStream);
        Task DeleteConfigurationAsync(int userId, string clusterId);
    }
}