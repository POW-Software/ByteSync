using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ByteSync.Common.Helpers;

public static class CryptographyUtils
{
    // This constant is used to determine the keysize of the encryption algorithm in bits.
    // We divide this by 8 within the code below to get the equivalent number of bytes.
    private const int Keysize = 128;

    // This constant determines the number of iterations for the password bytes generation function.
    private const int DerivationIterations = 1000;

    public static string ComputeSHA256(string fileInfoFullName)
    {
        using FileStream filestream = new FileStream(fileInfoFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ComputeSHA256(filestream);
    }

    public static string ComputeSHA256(Stream stream)
    {
        using var sha256 = SHA256.Create();
        stream.Position = 0;

        byte[] hashValue = sha256.ComputeHash(stream);

        string result = BitConverter.ToString(hashValue).Replace("-", String.Empty);

        return result;
    }
        
    public static string ComputeSHA256(byte[] bytes)
    {
        using var sha256 = SHA256.Create();

        byte[] hashValue = sha256.ComputeHash(bytes);

        string result = BitConverter.ToString(hashValue).Replace("-", String.Empty);

        return result;
    }
        
    public static string ComputeSHA256FromText(string text)
    {
        using var sha256 = SHA256.Create();

        byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            
        string result = BitConverter.ToString(hashValue).Replace("-", String.Empty);

        return result;
    }
        
    public static string ComputeSHA512(byte[] bytes)
    {
        using var sha512 = SHA512.Create();

        byte[] hashValue = sha512.ComputeHash(bytes);

        string result = BitConverter.ToString(hashValue).Replace("-", String.Empty);

        return result;
    }
        
    public static string ComputeMD5FromText(string text)
    {
        // Use input string to calculate MD5 hash
        using MD5 md5 = MD5.Create();

        byte[] hashValue = md5.ComputeHash(Encoding.UTF8.GetBytes(text));

        string result = BitConverter.ToString(hashValue).Replace("-", String.Empty);

        return result;
    }
        
    public static string ComputeSHA1(byte[] bytes)
    {
        using var sha1 = SHA1.Create();
            
        byte[] hashValue = sha1.ComputeHash(bytes);
            
        string result = BitConverter.ToString(hashValue).Replace("-", String.Empty);

        return result;
    }
        
    public static string Encrypt(string plainText, string passPhrase)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
            
        var result = EncryptBytes(bytes, passPhrase);

        return result;
    }
        
    public static string Decrypt(string cipherText, string passPhrase)
    {
        var bytes = DecryptBytes(cipherText, passPhrase);
            
        var result = Encoding.UTF8.GetString(bytes);

        return result;
    }
        
    public static string EncryptBytes(byte[] bytes, string passPhrase)
    {
        // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        var saltStringBytes = Generate128BitsOfRandomEntropy();
        var ivStringBytes = Generate128BitsOfRandomEntropy();
        // var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 128; // 128 uiquement en .NET Core : https://docs.microsoft.com/fr-fr/dotnet/api/system.security.cryptography.rijndaelmanaged.blocksize?view=net-5.0
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                symmetricKey.KeySize = 256;
                    
                using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(bytes, 0, bytes.Length);
                            cryptoStream.FlushFinalBlock();
                            // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                            var cipherTextBytes = saltStringBytes;
                            cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                            memoryStream.Close();
                            cryptoStream.Close();
                            return Convert.ToBase64String(cipherTextBytes);
                        }
                    }
                }
            }
        }
    }

    public static byte[] DecryptBytes(string cipherText, string passPhrase)
    {
        // Get the complete stream of bytes that represent:
        // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
        var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
        // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
        var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
        // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
        var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
        // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
        var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 128; // 128 uiquement en .NET Core : https://docs.microsoft.com/fr-fr/dotnet/api/system.security.cryptography.rijndaelmanaged.blocksize?view=net-5.0
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                symmetricKey.KeySize = 256;

                byte[] plainBytes;
                using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream(cipherTextBytes))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (var output = new MemoryStream())
                            {
                                cryptoStream.CopyTo(output);
                                plainBytes = output.ToArray();
                            }
                        }
                    }
                }

                return plainBytes;
            }
        }
    }

    private static byte[] Generate128BitsOfRandomEntropy()
    {
        var randomBytes = new byte[16]; // 16 Bytes will give us 128 bits.
        using (var rngCsp = new RNGCryptoServiceProvider())
        {
            // Fill the array with cryptographically secure random bytes.
            rngCsp.GetBytes(randomBytes);
        }
        return randomBytes;
    }
}