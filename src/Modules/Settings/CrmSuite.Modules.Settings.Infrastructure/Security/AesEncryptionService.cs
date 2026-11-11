using System.Security.Cryptography;
using System.Text;
using CrmSuite.Modules.Settings.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CrmSuite.Modules.Settings.Infrastructure.Security;

public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var secret = configuration["Settings:EncryptionKey"]
            ?? "CHANGE_ME_SETTINGS_ENCRYPTION_KEY_32_BYTES";

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        var parts = cipherText.Split(':', 2);
        if (parts.Length != 2)
        {
            throw new CryptographicException("Invalid cipher text.");
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = Convert.FromBase64String(parts[0]);

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(parts[1]);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
