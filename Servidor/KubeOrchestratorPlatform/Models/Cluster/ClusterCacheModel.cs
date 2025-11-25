namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class ClusterCache
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;

        public int CredentialId { get; set; }
        public string? Token { get; set; }
        public string? CertificateAuthorityData { get; set; }
        public string? ClientCertificateData { get; set; }
        public string? ClientKeyData { get; set; }
        public string AuthType { get; set; } = "Bearer";
    }
}