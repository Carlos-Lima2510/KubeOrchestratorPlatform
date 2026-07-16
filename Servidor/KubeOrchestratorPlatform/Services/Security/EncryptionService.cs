using System.Security.Cryptography;
using System.Text;

namespace Servidor.KubeOrchestratorPlatform.Services.Security
{
    public class EncryptionService : IEncryptionService
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly IConfiguration configuration;
        public EncryptionService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public string CurrentVersion => configuration["Encryption:CurrentVersion"]
            ?? throw new InvalidOperationException("No se ha definido 'Encryption:CurrentVersion' en la configuración.");
        public string Encrypt(string plainText)
        {
            return Encrypt(plainText, CurrentVersion);
        }
        public string Decrypt(string cipherText, string version)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            var key = GetKeyForVersion(version);
            var data = Convert.FromBase64String(cipherText);

            var nonce = data.AsSpan(0, NonceSize);
            var tag = data.AsSpan(data.Length - TagSize, TagSize);
            var cipherBytes = data.AsSpan(NonceSize, data.Length - NonceSize - TagSize);
            var plainBytes = new byte[cipherBytes.Length];

            try
            {
                using var aesGcm = new AesGcm(key, TagSize);
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Error de seguridad: No se pudieron desencriptar los datos.");
            }
        }
        private string Encrypt(string plainText, string version)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            var key = GetKeyForVersion(version);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var result = new byte[NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);

            return Convert.ToBase64String(result);
        }
        private byte[] GetKeyForVersion(string version)
        {
            var keyString = configuration[$"Encryption:Keys:{version}:Key"];

            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException($"No se encontró clave de encriptación para la versión '{version}'. Verifique su configuración.");
            }

            return Convert.FromBase64String(keyString);
        }
    }
}