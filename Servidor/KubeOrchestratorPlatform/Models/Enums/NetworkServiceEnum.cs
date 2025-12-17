using System.Text.Json.Serialization;

namespace Servidor.KubeOrchestratorPlatform.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ServiceType
    {
        ClusterIP,
        NodePort,
        LoadBalancer,
        ExternalName
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SessionAffinity
    {
        None,
        ClientIP
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TrafficPolicy
    {
        Cluster,
        Local
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IpFamilyPolicy
    {
        SingleStack,
        PreferDualStack,
        RequireDualStack
    }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Protocol
    {
        TCP,
        UDP,
        SCTP
    }
}