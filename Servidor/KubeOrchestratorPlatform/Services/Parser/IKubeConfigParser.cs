using Servidor.KubeOrchestratorPlatform.Models;

namespace Servidor.KubeOrchestratorPlatform.Services.Parser
{
    public interface IKubeConfigParser
    {
        ParsedKubeConfig Parse(string yamlContent);
    }
}