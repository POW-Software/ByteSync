using System.Security.Cryptography;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services.Encryptions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Encryptions;

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
        
        sessionSettings = SessionSettings.BuildDefault();
        
        // Encryption 1
        encryptedSessionSettings1 = _dataEncrypter.EncryptSessionSettings(sessionSettings);
        
        // Encryption 2
        encryptedSessionSettings2 = _dataEncrypter.EncryptSessionSettings(sessionSettings);
        
        encryptedSessionSettings1.Data.Length.Should().BeGreaterThan(0);
        encryptedSessionSettings1.IV.Length.Should().BeGreaterThan(0);
        
        encryptedSessionSettings2.Data.Length.Should().BeGreaterThan(0);
        encryptedSessionSettings2.IV.Length.Should().BeGreaterThan(0);
        
        encryptedSessionSettings1.Data.Should().Equal(encryptedSessionSettings1.Data);
        encryptedSessionSettings1.IV.Should().Equal(encryptedSessionSettings1.IV);
        
        encryptedSessionSettings1.Data.Should().NotEqual(encryptedSessionSettings2.Data);
        encryptedSessionSettings1.IV.Should().NotEqual(encryptedSessionSettings2.IV);
        
        // Decryption 1
        sessionSettings1 = _dataEncrypter.DecryptSessionSettings(encryptedSessionSettings1);
        
        // Decryption 2
        sessionSettings2 = _dataEncrypter.DecryptSessionSettings(encryptedSessionSettings2);
        
        // Test some properties
        sessionSettings.Extensions.Should().BeEquivalentTo(sessionSettings1.Extensions);
        sessionSettings.AllowedExtensions.Should().BeEquivalentTo(sessionSettings1.AllowedExtensions);
        sessionSettings.AllowedExtensions.HaveSameContent(sessionSettings1.AllowedExtensions).Should().BeTrue();
        sessionSettings.AnalysisMode.Should().Be(sessionSettings1.AnalysisMode);
        sessionSettings.DataType.Should().Be(sessionSettings1.DataType);
        sessionSettings.LinkingCase.Should().Be(sessionSettings1.LinkingCase);
        sessionSettings.LinkingKey.Should().Be(sessionSettings1.LinkingKey);
        sessionSettings.ForbiddenExtensions.Should().BeEquivalentTo(sessionSettings1.ForbiddenExtensions);
        sessionSettings.ForbiddenExtensions.HaveSameContent(sessionSettings1.ForbiddenExtensions).Should().BeTrue();
        
        // Test some properties
        sessionSettings.Extensions.Should().BeEquivalentTo(sessionSettings2.Extensions);
        sessionSettings.AllowedExtensions.Should().BeEquivalentTo(sessionSettings2.AllowedExtensions);
        sessionSettings.AllowedExtensions.HaveSameContent(sessionSettings2.AllowedExtensions).Should().BeTrue();
        sessionSettings.AnalysisMode.Should().Be(sessionSettings2.AnalysisMode);
        sessionSettings.DataType.Should().Be(sessionSettings2.DataType);
        sessionSettings.LinkingCase.Should().Be(sessionSettings2.LinkingCase);
        sessionSettings.LinkingKey.Should().Be(sessionSettings2.LinkingKey);
        sessionSettings.ForbiddenExtensions.Should().BeEquivalentTo(sessionSettings2.ForbiddenExtensions);
        sessionSettings.ForbiddenExtensions.HaveSameContent(sessionSettings2.ForbiddenExtensions).Should().BeTrue();
    }
}