namespace Servidor.KubeOrchestratorPlatform.Models.Enums
{
    public enum SecretType
    {
        Opaque,
        TlsSecret,
        DockerConfigJson,
        BasicAuth,
        SshAuth,
        ServiceAccountToken,
        BootstrapToken
    }
}