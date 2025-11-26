using k8s;
using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Factories.Client
{
    public interface IKubernetesClientFactory
    {
        IKubernetes CreateClient(ParsedKubeConfig config);
    }
}