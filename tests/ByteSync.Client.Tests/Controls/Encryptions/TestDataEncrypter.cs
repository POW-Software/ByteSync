using System.Linq;
using System.Security.Cryptography;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Services.Encryptions;
using ByteSync.Tests.TestUtilities.Helpers;
using ByteSync.TestsCommon;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Controls.Encryptions;

[TestFixture]
public class TestDataEncrypter : AbstractTester
{
    private Mock<ICloudSessionConnectionRepository> _mockCloudSessionConnectionDataHolder;
    private DataEncrypter _dataEncrypter;

    [SetUp]
    public void SetUp()
    {
        _mockCloudSessionConnectionDataHolder = new Mock<ICloudSessionConnectionRepository>();

        _dataEncrypter = new DataEncrypter(
            _mockCloudSessionConnectionDataHolder.Object
        );
    }
    
    [Test]
    public void TestSessionSettings()
    {
        SessionSettings sessionSettings, sessionSettings1, sessionSettings2;
        EncryptedSessionSettings encryptedSessionSettings1, encryptedSessionSettings2;
        
        _mockCloudSessionConnectionDataHolder.Setup(x => x.GetAesEncryptionKey()).Returns(Aes.Create().Key);
        
        // DataEncrypter dataEncrypter = new DataEncrypter(generator.CloudSessionConnectionDataHolder.Object);
        sessionSettings = SessionSettings.BuildDefault();
        
        // Encryptage 1
        encryptedSessionSettings1 = _dataEncrypter.EncryptSessionSettings(sessionSettings);
        
        // Encryptage 2
        encryptedSessionSettings2 = _dataEncrypter.EncryptSessionSettings(sessionSettings);
        
        ClassicAssert.IsTrue(encryptedSessionSettings1.Data.Length > 0);
        ClassicAssert.IsTrue(encryptedSessionSettings1.IV.Length > 0);
        
        ClassicAssert.IsTrue(encryptedSessionSettings2.Data.Length > 0);
        ClassicAssert.IsTrue(encryptedSessionSettings2.IV.Length > 0);
        
        ClassicAssert.IsTrue(encryptedSessionSettings1.Data.SequenceEqual(encryptedSessionSettings1.Data));
        ClassicAssert.IsTrue(encryptedSessionSettings1.IV.SequenceEqual(encryptedSessionSettings1.IV));
        
        ClassicAssert.IsFalse(encryptedSessionSettings1.Data.SequenceEqual(encryptedSessionSettings2.Data));
        ClassicAssert.IsFalse(encryptedSessionSettings1.IV.SequenceEqual(encryptedSessionSettings2.IV));
        
        // Décryptage 1
        sessionSettings1 = _dataEncrypter.DecryptSessionSettings(encryptedSessionSettings1);
        
        // Décryptage 2
        sessionSettings2 = _dataEncrypter.DecryptSessionSettings(encryptedSessionSettings2);
        
        // On teste certaines propriétés
        ClassicAssert.AreEqual(sessionSettings.Extensions, sessionSettings1.Extensions);
        ClassicAssert.AreEqual(sessionSettings.AllowedExtensions, sessionSettings1.AllowedExtensions);
        ClassicAssert.IsTrue(sessionSettings.AllowedExtensions.HaveSameContent(sessionSettings1.AllowedExtensions));
        ClassicAssert.AreEqual(sessionSettings.AnalysisMode, sessionSettings1.AnalysisMode);
        ClassicAssert.AreEqual(sessionSettings.DataType, sessionSettings1.DataType);
        ClassicAssert.AreEqual(sessionSettings.LinkingCase, sessionSettings1.LinkingCase);
        ClassicAssert.AreEqual(sessionSettings.LinkingKey, sessionSettings1.LinkingKey);
        ClassicAssert.AreEqual(sessionSettings.ForbiddenExtensions, sessionSettings1.ForbiddenExtensions);
        ClassicAssert.IsTrue(sessionSettings.ForbiddenExtensions.HaveSameContent(sessionSettings1.ForbiddenExtensions));
        
        // On teste certaines propriétés
        ClassicAssert.AreEqual(sessionSettings.Extensions, sessionSettings2.Extensions);
        ClassicAssert.AreEqual(sessionSettings.AllowedExtensions, sessionSettings2.AllowedExtensions);
        ClassicAssert.IsTrue(sessionSettings.AllowedExtensions.HaveSameContent(sessionSettings2.AllowedExtensions));
        ClassicAssert.AreEqual(sessionSettings.AnalysisMode, sessionSettings2.AnalysisMode);
        ClassicAssert.AreEqual(sessionSettings.DataType, sessionSettings2.DataType);
        ClassicAssert.AreEqual(sessionSettings.LinkingCase, sessionSettings2.LinkingCase);
        ClassicAssert.AreEqual(sessionSettings.LinkingKey, sessionSettings2.LinkingKey);
        ClassicAssert.AreEqual(sessionSettings.ForbiddenExtensions, sessionSettings2.ForbiddenExtensions);
        ClassicAssert.IsTrue(sessionSettings.ForbiddenExtensions.HaveSameContent(sessionSettings2.ForbiddenExtensions));
    }
}