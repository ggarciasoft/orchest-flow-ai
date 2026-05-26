using System.Security.Cryptography;
using System.Text;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.Infrastructure.Security;

/// <summary>
/// AES-256-CBC encryption service for sensitive values at rest.
/// Master key comes from config key "Encryption:MasterKey" or env ENCRYPTION_MASTER_KEY.
/// Key is SHA-256 hashed to produce a 32-byte AES key.
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(string masterKey)
    {
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(masterKey));
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        // Format: base64(iv) + ":" + base64(ciphertext)
        return Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(cipher);
    }

    public string Decrypt(string ciphertext)
    {
        var parts = ciphertext.Split(':', 2);
        if (parts.Length != 2) throw new InvalidOperationException("Invalid ciphertext format");
        var iv = Convert.FromBase64String(parts[0]);
        var cipher = Convert.FromBase64String(parts[1]);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
