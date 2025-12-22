using System.Text.Json.Serialization;

namespace Servidor.KubeOrchestratorPlatform.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IngressPathType
    {
        Prefix,
        Exact,
        ImplementationSpecific
    }
}
