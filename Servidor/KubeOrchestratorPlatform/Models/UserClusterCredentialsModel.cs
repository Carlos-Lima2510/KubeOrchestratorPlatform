namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class UserClusterCredentials
    {
        public string? Server { get; set; }
        public string? CertificateAuthorityData { get; set; }
        public string? Token { get; set; }
        public string? ClientCertificateData { get; set; }
        public string? ClientKeyData { get; set; }
        public string? AuthType { get; set;}
    }
}