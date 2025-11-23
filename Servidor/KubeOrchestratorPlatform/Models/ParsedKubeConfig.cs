namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class ParsedKubeConfig
    {
        public string? CurrentContext { get; set; }
        public string? Server { get; set; }

        public string? CertificateAuthorityData { get; set; }

        public string? Token { get; set; }

        public string? ClientCertificateData { get; set; }
        public string? ClientKeyData { get; set; }
        public string? AuthType { get; set; }
    }
}