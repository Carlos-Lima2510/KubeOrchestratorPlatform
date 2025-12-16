using System.Text.Json.Serialization;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StrategyType
    {
        RollingUpdate,
        Recreate
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VolumeType
    {
        EmptyDir,
        PersistentVolumeClaim,
        ConfigMap,
        Secret
    }
}