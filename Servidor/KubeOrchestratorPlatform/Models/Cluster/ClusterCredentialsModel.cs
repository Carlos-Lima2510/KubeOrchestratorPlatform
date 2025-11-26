namespace Servidor.KubeOrchestratorPlatform.Models
{
    public class ClusterCredentials
    {
        public int Id { get; set; }

        public int ConnectedClusterId { get; set; }
        public ConnectedCluster ConnectedCluster { get; set; } = null!;

        public string? CertificateAuthorityData { get; set; }
        public string? Token { get; set; }
        public string? ClientCertificateData { get; set; }
        public string? ClientKeyData { get; set; }

        public string? AuthType { get; set;}

    }
}