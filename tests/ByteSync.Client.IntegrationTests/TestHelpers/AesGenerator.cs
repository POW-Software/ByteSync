using System.Security.Cryptography;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

class AesGenerator
{
    internal static void GenerateKeyAndIV(out byte[] key, out byte[] iv)
    {
        using (AesManaged aesManaged = new AesManaged())
        {
            aesManaged.GenerateKey();
            aesManaged.GenerateIV();

            key = aesManaged.Key;
            iv = aesManaged.IV;
        }
    }

    internal static byte[] GenerateKey()
    {
        using (AesManaged aesManaged = new AesManaged())
        {
            aesManaged.GenerateKey();

            byte[] key = aesManaged.Key;

            return key;
        }
    }

    internal static byte[] GenerateIV()
    {
        using (AesManaged aesManaged = new AesManaged())
        {
            aesManaged.GenerateKey();

            byte[] iv = aesManaged.IV;

            return iv;
        }
    }
}