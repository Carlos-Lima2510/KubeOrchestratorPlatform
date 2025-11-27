namespace Servidor.KubeOrchestratorPlatform.Services.Security
{
    public interface IEncryptionService
    {
        string CurrentVersion { get; }
        string Encrypt(string plainText);
        string Decrypt(string cipherText, string version);
    }
}