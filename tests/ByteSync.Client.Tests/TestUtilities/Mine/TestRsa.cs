using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.TestUtilities.Mine;

[TestFixture]
public class TestRsa
{
    [Test]
    public void Test1_DifferentMessages()
    {
        List<byte[]> publicKeys = new List<byte[]>();
        List<byte[]> encryptedMessages = new List<byte[]>();

        for (int i = 0; i < 100; i++)
        {
            RSA rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter

            
            RSA rsaAliceImported = RSA.Create();
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();

            ClassicAssert.IsFalse(publicKeys.Any(pk => pk.SequenceEqual(rsaBobPublicKey))); // On ne doit pas encore avoir eu affaire à cette publicKey
            publicKeys.Add(rsaBobPublicKey);

            rsaAliceImported.ImportRSAPublicKey(rsaBobPublicKey, out _);
            string aliceMessage = $"messageTestAlice_{i}";

            var bytes = Encoding.UTF8.GetBytes(aliceMessage);
            var encryptedMessage = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);

            ClassicAssert.IsFalse(encryptedMessages.Any(em => em.SequenceEqual(encryptedMessage))); // On ne doit pas encore avoir eu affaire à cette publicKey
            encryptedMessages.Add(encryptedMessage);

            var decryptedBytes = rsaBob.Decrypt(encryptedMessage, RSAEncryptionPadding.Pkcs1);
            var decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
            
            ClassicAssert.AreEqual(aliceMessage, decryptedMessage);
        }

        ClassicAssert.AreEqual(100, publicKeys.Count);
        ClassicAssert.AreEqual(100, encryptedMessages.Count);
            
        // On vérifie que le contrôle qu'on appliquait fonctionne correctement
        ClassicAssert.IsTrue(publicKeys.Any(pk => pk.SequenceEqual(publicKeys[50])));
        ClassicAssert.IsTrue(encryptedMessages.Any(em => em.SequenceEqual(encryptedMessages[50])));
    }
        
    [Test]
    public void Test1_SameMessages()
    {
        List<byte[]> publicKeys = new List<byte[]>();
        List<byte[]> encryptedMessages = new List<byte[]>();

        for (int i = 0; i < 100; i++)
        {
            RSA rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter

            
            RSA rsaAliceImported = RSA.Create();
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();

            ClassicAssert.IsFalse(publicKeys.Any(pk => pk.SequenceEqual(rsaBobPublicKey))); // On ne doit pas encore avoir eu affaire à cette publicKey
            publicKeys.Add(rsaBobPublicKey);

            rsaAliceImported.ImportRSAPublicKey(rsaBobPublicKey, out _);
            string aliceMessage = $"messageTestAlice_A";

            var bytes = Encoding.UTF8.GetBytes(aliceMessage);
            var encryptedMessage = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);

            ClassicAssert.IsFalse(encryptedMessages.Any(em => em.SequenceEqual(encryptedMessage))); // On ne doit pas encore avoir eu affaire à cette publicKey
            encryptedMessages.Add(encryptedMessage);

            var decryptedBytes = rsaBob.Decrypt(encryptedMessage, RSAEncryptionPadding.Pkcs1);
            var decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
            
            ClassicAssert.AreEqual(aliceMessage, decryptedMessage);
        }

        ClassicAssert.AreEqual(100, publicKeys.Count);
        ClassicAssert.AreEqual(100, encryptedMessages.Count);
            
        // On vérifie que le contrôle qu'on appliquait fonctionne correctement
        ClassicAssert.IsTrue(publicKeys.Any(pk => pk.SequenceEqual(publicKeys[50])));
        ClassicAssert.IsTrue(encryptedMessages.Any(em => em.SequenceEqual(encryptedMessages[50])));
    }
        
    [Test]
    public void Test1_PublicKeyUnicity()
    {
        for (int i = 0; i < 100; i++)
        {
            RSA rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter
                
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();
            var rsaBobPublicKey2 = rsaBob.ExportRSAPublicKey();
                
            ClassicAssert.IsTrue(rsaBobPublicKey.SequenceEqual(rsaBobPublicKey2));
        }
    }
        
    [Test]
    public void Test1_EncryptedMessageNonUnicity()
    {
        for (int i = 0; i < 100; i++)
        {
            RSA rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter

            
            RSA rsaAliceImported = RSA.Create();
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();

            rsaAliceImported.ImportRSAPublicKey(rsaBobPublicKey, out _);
            string aliceMessage = $"messageTestAlice_{i}";

            var bytes = Encoding.UTF8.GetBytes(aliceMessage);
            var encryptedMessage1 = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            var encryptedMessage2 = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
                
            ClassicAssert.IsFalse(encryptedMessage1.SequenceEqual(encryptedMessage2));
        }
    }
}