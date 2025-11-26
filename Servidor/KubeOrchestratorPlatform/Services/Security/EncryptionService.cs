using System.Security.Cryptography;
using System.Text;

namespace Servidor.KubeOrchestratorPlatform.Services.Security
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] key;
        private readonly byte[] iv;
        public EncryptionService(IConfiguration configuration)
        {
            var keyString = configuration["Encryption:Key"];
            var ivString = configuration["Encryption:IV"];

            key = Convert.FromBase64String(keyString!);
            iv = Convert.FromBase64String(ivString!);
        }
        public string Encrypt(string plainText)
        {
            if(string.IsNullOrEmpty(plainText)) return plainText;

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
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);

            return streamReader.ReadToEnd();
        }

    }
}