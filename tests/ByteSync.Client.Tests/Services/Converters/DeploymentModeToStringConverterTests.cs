using System.Globalization;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services.Converters;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Converters;

[TestFixture]
public class DeploymentModeToStringConverterTests
{
    private static DeploymentModeToStringConverter CreateConverter(out Mock<ILocalizationService> loc)
    {
        loc = new Mock<ILocalizationService>();

        loc.Setup(l => l["DeploymentMode_Portable"])
            .Returns("Portable-Loc");
        loc.Setup(l => l["DeploymentMode_Installation"])
            .Returns("Installation-Loc");
        loc.Setup(l => l["DeploymentMode_MSIX"])
            .Returns("MSIX-Loc");

        return new DeploymentModeToStringConverter(loc.Object);
    }

    [Test]
    public void Convert_Should_Return_Portable_Text_For_Portable_Mode()
    {
        var converter = CreateConverter(out _);

        var result = converter.Convert(DeploymentMode.Portable, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("Portable-Loc");
    }

    [Test]
    public void Convert_Should_Return_Installation_Text_For_Setup_Mode()
    {
        var converter = CreateConverter(out _);

        var result = converter.Convert(DeploymentMode.SetupInstallation, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("Installation-Loc");
    }

    [Test]
    public void Convert_Should_Return_MSIX_Text_For_Msix_Mode()
    {
        var converter = CreateConverter(out _);

        var result = converter.Convert(DeploymentMode.MsixInstallation, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("MSIX-Loc");
    }

    [Test]
    public void Convert_Should_Return_Empty_For_Unsupported_Value_Type()
    {
        var converter = CreateConverter(out _);

        var result = converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Test]
    public void ConvertBack_Is_Not_Supported_And_Returns_Null()
    {
        var converter = CreateConverter(out _);

        var result = converter.ConvertBack("any", typeof(DeploymentMode), null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }
}