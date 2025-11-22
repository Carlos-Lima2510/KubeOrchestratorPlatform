using k8s;

namespace Servidor.KubeOrchestratorPlatform.Kubernetes
{
    public interface IKubeConnection
    {
        IKubernetes GetClient();
        string Namespace { get;}
    }
}
