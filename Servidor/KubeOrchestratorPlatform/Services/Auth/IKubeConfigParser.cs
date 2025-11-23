using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Auth
{
    public interface IKubeConfigParser
    {
        ParsedKubeConfig Parse(string yamlContent);
    }
}