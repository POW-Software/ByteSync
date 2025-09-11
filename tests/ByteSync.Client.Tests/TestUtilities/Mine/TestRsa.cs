using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.TestUtilities.Mine;

[TestFixture]
public class TestRsa
{
    [Test]
    public void Test1_DifferentMessages()
    {
        var publicKeys = new List<byte[]>();
        var encryptedMessages = new List<byte[]>();

        for (int i = 0; i < 100; i++)
        {
            var rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter

            
            var rsaAliceImported = RSA.Create();
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();

            publicKeys.Any(pk => pk.SequenceEqual(rsaBobPublicKey)).Should().BeFalse(); // On ne doit pas encore avoir eu affaire à cette publicKey
            publicKeys.Add(rsaBobPublicKey);

            rsaAliceImported.ImportRSAPublicKey(rsaBobPublicKey, out _);
            var aliceMessage = $"messageTestAlice_{i}";

            var bytes = Encoding.UTF8.GetBytes(aliceMessage);
            var encryptedMessage = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);

            encryptedMessages.Any(em => em.SequenceEqual(encryptedMessage)).Should().BeFalse(); // On ne doit pas encore avoir eu affaire à cette publicKey
            encryptedMessages.Add(encryptedMessage);

            var decryptedBytes = rsaBob.Decrypt(encryptedMessage, RSAEncryptionPadding.Pkcs1);
            var decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
            
            decryptedMessage.Should().Be(aliceMessage);
        }

        publicKeys.Count.Should().Be(100);
        encryptedMessages.Count.Should().Be(100);
            
        // On vérifie que le contrôle qu'on appliquait fonctionne correctement
        publicKeys.Any(pk => pk.SequenceEqual(publicKeys[50])).Should().BeTrue();
        encryptedMessages.Any(em => em.SequenceEqual(encryptedMessages[50])).Should().BeTrue();
    }
        
    [Test]
    public void Test1_SameMessages()
    {
        var publicKeys = new List<byte[]>();
        var encryptedMessages = new List<byte[]>();

        for (int i = 0; i < 100; i++)
        {
            var rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter

            
            var rsaAliceImported = RSA.Create();
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();

            publicKeys.Any(pk => pk.SequenceEqual(rsaBobPublicKey)).Should().BeFalse(); // On ne doit pas encore avoir eu affaire à cette publicKey
            publicKeys.Add(rsaBobPublicKey);

            rsaAliceImported.ImportRSAPublicKey(rsaBobPublicKey, out _);
            var aliceMessage = $"messageTestAlice_A";

            var bytes = Encoding.UTF8.GetBytes(aliceMessage);
            var encryptedMessage = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);

            encryptedMessages.Any(em => em.SequenceEqual(encryptedMessage)).Should().BeFalse(); // On ne doit pas encore avoir eu affaire à cette publicKey
            encryptedMessages.Add(encryptedMessage);

            var decryptedBytes = rsaBob.Decrypt(encryptedMessage, RSAEncryptionPadding.Pkcs1);
            var decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
            
            decryptedMessage.Should().Be(aliceMessage);
        }

        publicKeys.Count.Should().Be(100);
        encryptedMessages.Count.Should().Be(100);
            
        // On vérifie que le contrôle qu'on appliquait fonctionne correctement
        publicKeys.Any(pk => pk.SequenceEqual(publicKeys[50])).Should().BeTrue();
        encryptedMessages.Any(em => em.SequenceEqual(encryptedMessages[50])).Should().BeTrue();
    }
        
    [Test]
    public void Test1_PublicKeyUnicity()
    {
        for (int i = 0; i < 100; i++)
        {
            var rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter
                
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();
            var rsaBobPublicKey2 = rsaBob.ExportRSAPublicKey();
                
            rsaBobPublicKey.SequenceEqual(rsaBobPublicKey2).Should().BeTrue();
        }
    }
        
    [Test]
    public void Test1_EncryptedMessageNonUnicity()
    {
        for (int i = 0; i < 100; i++)
        {
            var rsaBob = RSA.Create(); // Alice peut encrypter, Bob peut décrypter

            
            var rsaAliceImported = RSA.Create();
            var rsaBobPublicKey = rsaBob.ExportRSAPublicKey();

            rsaAliceImported.ImportRSAPublicKey(rsaBobPublicKey, out _);
            var aliceMessage = $"messageTestAlice_{i}";

            var bytes = Encoding.UTF8.GetBytes(aliceMessage);
            var encryptedMessage1 = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            var encryptedMessage2 = rsaAliceImported.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
                
            encryptedMessage1.SequenceEqual(encryptedMessage2).Should().BeFalse();
        }
    }
}