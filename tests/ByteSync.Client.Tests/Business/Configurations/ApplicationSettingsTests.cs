using ByteSync.Business.Configurations;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Business.Configurations;

public class ApplicationSettingsTests
{
	[Test]
	public void InitializeRsa_GeneratesKeys_And_SetsClientIdWithExpectedFormat()
	{
		var settings = new ApplicationSettings();
		settings.SetEncryptionPassword("test-password");
		settings.InstallationId = "InstallationId_TEST";

		settings.InitializeRsa();

		settings.DecodedRsaPrivateKey.Should().NotBeNull();
		settings.DecodedRsaPrivateKey!.Length.Should().BeGreaterThan(0);
		settings.DecodedRsaPublicKey.Should().NotBeNull();
		settings.DecodedRsaPublicKey!.Length.Should().BeGreaterThan(0);

		settings.ClientId.Should().MatchRegex("^[A-Z]{4}-[A-Z]{3}-[A-Z]{3}$");
	}

	[Test]
	public void InitializeRsa_Twice_WithDifferentInstallationId_ProducesDifferentClientIds()
	{
		var settings1 = new ApplicationSettings();
		settings1.SetEncryptionPassword("pw");
		settings1.InstallationId = "InstallationId_A";
		settings1.InitializeRsa();
		var clientId1 = settings1.ClientId;

		var settings2 = new ApplicationSettings();
		settings2.SetEncryptionPassword("pw");
		settings2.InstallationId = "InstallationId_B";
		settings2.InitializeRsa();
		var clientId2 = settings2.ClientId;

		clientId1.Should().NotBeNullOrEmpty();
		clientId2.Should().NotBeNullOrEmpty();
		clientId1.Should().NotBe(clientId2);
	}
}


