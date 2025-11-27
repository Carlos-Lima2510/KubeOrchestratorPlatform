using System.Security.Cryptography;

namespace Servidor.KubeOrchestratorPlatform.Services.Security
{
    public class EncryptionService : IEncryptionService
    {
        private readonly IConfiguration configuration;
        public EncryptionService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public string CurrentVersion => configuration["Encryption:CurrentVersion"]
            ?? throw new InvalidOperationException("No se ha definido 'Encryption:CurrentVersion' en la configuración.");
        public string Encrypt(string plainText)
        {
            return EncryptWithVersion(plainText, CurrentVersion);
        }
        public string Decrypt(string cipherText, string version)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            var (key, iv) = GetKeysForVersion(version);

            try
            {
                

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var memoryStream = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return streamReader.ReadToEnd();
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Error de seguridad: No se pudieron desencriptar los datos.");
            }
        }
        private string EncryptWithVersion(string plainText, string version)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            var (key, iv) = GetKeysForVersion(version);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using var streamWriter = new StreamWriter(cryptoStream);
            
            streamWriter.Write(plainText);
            streamWriter.Close(); 

            return Convert.ToBase64String(memoryStream.ToArray());
        }
        private (byte[] Key, byte[] IV) GetKeysForVersion(string version)
        {
            var keyString = configuration[$"Encryption:Keys:{version}:Key"];
            var ivString = configuration[$"Encryption:Keys:{version}:IV"];

            if (string.IsNullOrEmpty(keyString) || string.IsNullOrEmpty(ivString))
            {
                throw new InvalidOperationException($"No se encontraron claves de encriptación para la versión '{version}'. Verifique su configuración.");
            }

            return (Convert.FromBase64String(keyString), Convert.FromBase64String(ivString));
        }
    }
}