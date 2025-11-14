using System.Security.Cryptography;

namespace CLDV6212POE.Services;

public class EncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(string hexKey, string hexIV)
    {
        _key = Convert.FromHexString(hexKey);
        _iv = Convert.FromHexString(hexIV);
    }

    // Helper method to perform encryption/decryption
    private byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
    }


    // Encrypts the input data and returns the encrypted byte array
    public byte[] Encrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;

            using (var encrypter = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                return PerformCryptography(data, encrypter);
            }
        }
    }


    // Decrypts the input data and returns the decrypted byte array
    public byte[] Decrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;

            using (var decrypter = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                return PerformCryptography(data, decrypter);
            }
        }
    }
}