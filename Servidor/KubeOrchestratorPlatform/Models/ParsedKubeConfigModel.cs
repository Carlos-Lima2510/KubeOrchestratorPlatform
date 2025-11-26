namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class ParsedKubeConfig
    {
        public string Server { get; set; } = null!;
        public string? ClusterName { get; set; } 
        public string? UserName { get; set; }


        public string? CertificateAuthorityData { get; set; }
        public string? Token { get; set; }
        public string? ClientCertificateData { get; set; }
        public string? ClientKeyData { get; set; }
        public string? AuthType { get; set; }
    }
}