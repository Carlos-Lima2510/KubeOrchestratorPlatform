using System.Text.Json.Serialization;

namespace Servidor.KubeOrchestratorPlatform.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SecretType
    {
        Opaque,
        TlsSecret,
        DockerConfigJson,
        BasicAuth,
        SshAuth,
        ServiceAccountToken,
        BootstrapToken
    }
}