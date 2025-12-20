using System.ComponentModel.DataAnnotations;
using Servidor.KubeOrchestratorPlatform.Models.Enums;

namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Network.Ingress
{
    public class CreateIngressDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Namespace { get; set; } = "default";
        public Dictionary<string, string> Annotations { get; set; } = new();

        public string? IngressClassName { get; set; }

        public IngressBackendDTO? DefaultBackend { get; set; }

        public List<IngressRuleDTO> Rules { get; set; } = new();

        public List<IngressTlsDTO> Tls { get; set; } = new();
    }

    public class IngressRuleDTO
    {
        public string? Host { get; set; }
        public List<IngressPathDTO> Paths { get; set; } = new();
    }

    public class IngressPathDTO
    {
        [Required]
        public string Path { get; set; } = "/";

        [Required]
        public IngressPathType PathType { get; set; } = IngressPathType.Prefix;

        [Required]
        public IngressBackendDTO Backend { get; set; } = new();
    }

    public class IngressBackendDTO
    {
        public IngressServiceBackendDTO? Service { get; set; }

        public IngressResourceBackendDTO? Resource { get; set; }
    }

    public class IngressServiceBackendDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public ServiceBackendPortDTO Port { get; set; } = new();
    }

    public class ServiceBackendPortDTO
    {
        public int? Number { get; set; }
        public string? Name { get; set; }
    }

    public class IngressResourceBackendDTO
    {
        [Required]
        public string ApiGroup { get; set; } = string.Empty;
        [Required]
        public string Kind { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class IngressTlsDTO
    {
        public List<string> Hosts { get; set; } = new();
        public string? SecretName { get; set; }
    }
}
