using System;
using System.Security.Cryptography;
using System.Text;

namespace RemoteTerminal.Server.Auth;

public static class AuthKeyCryptor
{
    private static readonly string SharedKey = nameof(AuthSettings) + "8FCA694FC63B43F78B66434676207B51";

    private const int AesKeyLength = 32; // AES-256
    private const int NonceLength = 12;  // AES-GCM standard nonce size
    private const int TagLength = 16;    // AES-GCM authentication tag size
    private const int MinEncryptedLength = NonceLength + TagLength + 1; // nonce + tag + at least 1 byte

    private static readonly byte[] HkdfInfo = "AuthKeyCryptor.AES-GCM.v1"u8.ToArray();

    private static byte[] DeriveAesKey()
    {
        var keyMaterial = Encoding.UTF8.GetBytes(SharedKey);
        try
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, keyMaterial, AesKeyLength, info: HkdfInfo);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyMaterial);
        }
    }

    public static string EncryptKey(AuthKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var authKeyBytes = key.ToBytes();
        var aesKey = DeriveAesKey();

        try
        {
            var nonce = new byte[NonceLength];
            RandomNumberGenerator.Fill(nonce);

            var cipherBytes = new byte[authKeyBytes.Length];
            var tag = new byte[TagLength];

            using var aesGcm = new AesGcm(aesKey, TagLength);
            aesGcm.Encrypt(nonce, authKeyBytes, cipherBytes, tag);

            // Layout: [nonce (12)] [tag (16)] [ciphertext (N)]
            var result = new byte[NonceLength + TagLength + cipherBytes.Length];
            nonce.CopyTo(result, 0);
            tag.CopyTo(result, NonceLength);
            cipherBytes.CopyTo(result, NonceLength + TagLength);

            return Convert.ToBase64String(result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(authKeyBytes);
        }
    }

    public static AuthKey DecryptKeyText(string encrypted)
    {
        if (string.IsNullOrWhiteSpace(encrypted))
        {
            throw new ArgumentException("The encrypted string must not be null or whitespace.", nameof(encrypted));
        }

        byte[] decryptedBytes;
        var aesKey = DeriveAesKey();

        try
        {
            var allBytes = Convert.FromBase64String(encrypted);

            if (allBytes.Length < MinEncryptedLength)
            {
                throw new InvalidOperationException(
                    "The encrypted data is too short to contain valid encrypted content.");
            }

            var nonce = allBytes[..NonceLength];
            var tag = allBytes[NonceLength..(NonceLength + TagLength)];
            var cipherBytes = allBytes[(NonceLength + TagLength)..];

            decryptedBytes = new byte[cipherBytes.Length];

            using var aesGcm = new AesGcm(aesKey, TagLength);
            aesGcm.Decrypt(nonce, cipherBytes, tag, decryptedBytes);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("The encrypted string could not be successfully decrypted.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
        }

        return AuthKeyHelper.FromBytes(decryptedBytes);
    }
}
