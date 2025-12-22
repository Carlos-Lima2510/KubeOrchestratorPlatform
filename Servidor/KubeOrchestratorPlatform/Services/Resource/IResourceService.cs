namespace Servidor.KubeOrchestratorPlatform.Services.Resources
{
    public interface IResourceService
    {
        Task<string> GetResourceYamlAsync(int userId, string clusterId, string ns, string type, string name);
    }
}