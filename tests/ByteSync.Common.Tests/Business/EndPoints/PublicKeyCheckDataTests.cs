using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Versions;
using FluentAssertions;
using NUnit.Framework;

namespace TestingCommon.Business.EndPoints;

[TestFixture]
public class PublicKeyCheckDataTests
{
    private PublicKeyInfo CreatePublicKeyInfo(string clientId, byte[] publicKey, int protocolVersion)
    {
        return new PublicKeyInfo
        {
            ClientId = clientId,
            PublicKey = publicKey,
            ProtocolVersion = protocolVersion
        };
    }
    
    private PublicKeyCheckData CreatePublicKeyCheckData(PublicKeyInfo issuerPublicKeyInfo, string issuerClientInstanceId)
    {
        return new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = issuerPublicKeyInfo,
            IssuerClientInstanceId = issuerClientInstanceId,
            Salt = "TestSalt123",
            ProtocolVersion = ProtocolVersion.CURRENT
        };
    }
    
    [Test]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        var data = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        
        // ReSharper disable once EqualExpressionComparison
        var result = data.Equals(data);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        var data = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        
        var result = data.Equals(null);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        var data = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        
        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = data.Equals("NotAPublicKeyCheckData");
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void Equals_WithEqualObjects_ShouldReturnTrue()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        var data2 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        
        var result = data1.Equals(data2);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void Equals_WithDifferentIssuerPublicKeyInfo_ShouldReturnFalse()
    {
        var publicKeyInfo1 = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        var publicKeyInfo2 = CreatePublicKeyInfo("Client2", "PublicKey2"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo1, "Instance1");
        var data2 = CreatePublicKeyCheckData(publicKeyInfo2, "Instance1");
        
        var result = data1.Equals(data2);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void Equals_WithDifferentIssuerClientInstanceId_ShouldReturnFalse()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        var data2 = CreatePublicKeyCheckData(publicKeyInfo, "Instance2");
        
        var result = data1.Equals(data2);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void Equals_WithDifferentOtherProperties_ShouldReturnTrue()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        data1.Salt = "Salt1";
        data1.ProtocolVersion = ProtocolVersion.CURRENT;
        data1.OtherPartyCheckResponse = true;
        
        var data2 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        data2.Salt = "Salt2";
        data2.ProtocolVersion = ProtocolVersion.CURRENT;
        data2.OtherPartyCheckResponse = false;
        
        var result = data1.Equals(data2);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void GetHashCode_WithEqualObjects_ShouldReturnSameHashCode()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        var data2 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        
        var hashCode1 = data1.GetHashCode();
        var hashCode2 = data2.GetHashCode();
        
        hashCode1.Should().Be(hashCode2);
    }
    
    [Test]
    public void GetHashCode_WithDifferentIssuerPublicKeyInfo_ShouldReturnDifferentHashCode()
    {
        var publicKeyInfo1 = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        var publicKeyInfo2 = CreatePublicKeyInfo("Client2", "PublicKey2"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo1, "Instance1");
        var data2 = CreatePublicKeyCheckData(publicKeyInfo2, "Instance1");
        
        var hashCode1 = data1.GetHashCode();
        var hashCode2 = data2.GetHashCode();
        
        hashCode1.Should().NotBe(hashCode2);
    }
    
    [Test]
    public void GetHashCode_WithDifferentIssuerClientInstanceId_ShouldReturnDifferentHashCode()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        var data2 = CreatePublicKeyCheckData(publicKeyInfo, "Instance2");
        
        var hashCode1 = data1.GetHashCode();
        var hashCode2 = data2.GetHashCode();
        
        hashCode1.Should().NotBe(hashCode2);
    }
    
    [Test]
    public void GetHashCode_WithDifferentOtherProperties_ShouldReturnSameHashCode()
    {
        var publicKeyInfo = CreatePublicKeyInfo("Client1", "PublicKey1"u8.ToArray(), ProtocolVersion.CURRENT);
        
        var data1 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        data1.Salt = "Salt1";
        data1.ProtocolVersion = ProtocolVersion.CURRENT;
        
        var data2 = CreatePublicKeyCheckData(publicKeyInfo, "Instance1");
        data2.Salt = "Salt2";
        data2.ProtocolVersion = ProtocolVersion.CURRENT;
        
        var hashCode1 = data1.GetHashCode();
        var hashCode2 = data2.GetHashCode();
        
        hashCode1.Should().Be(hashCode2);
    }
}