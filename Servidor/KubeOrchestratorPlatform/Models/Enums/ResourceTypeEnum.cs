using System.Text.Json.Serialization;

namespace Servidor.KubeOrchestratorPlatform.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourceTypeEnum
    {
        Ingress,
        Service,
        Pod
    }
}
