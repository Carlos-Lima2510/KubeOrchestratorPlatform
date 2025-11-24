using k8s;

namespace Servidor.KubeOrchestratorPlatform.Services.Connection
{
    public interface IKubernetesUserClientConnection
    {
        IKubernetes GetClient(int userId, string clusterId);
    }
}