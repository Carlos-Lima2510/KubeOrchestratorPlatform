namespace Servidor.KubeOrchestratorPlatform.Services.Security
{
    public interface INetworkSafetyValidator
    {
        Task EnsureSafeClusterEndpointAsync(string serverUrl);
    }
}
