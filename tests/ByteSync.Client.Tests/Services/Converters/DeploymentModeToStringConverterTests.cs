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
    private Mock<ILocalizationService> _mockLocalizationService = null!;
    private DeploymentModeToStringConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();

        _mockLocalizationService.Setup(x => x["DeploymentMode_Portable"])
            .Returns("Portable-Loc");
        _mockLocalizationService.Setup(x => x["DeploymentMode_Installation"])
            .Returns("Installation-Loc");
        _mockLocalizationService.Setup(x => x["DeploymentMode_MSIX"])
            .Returns("MSIX-Loc");

        _converter = new DeploymentModeToStringConverter(_mockLocalizationService.Object);
    }

    [Test]
    public void Convert_Should_Return_Portable_Text_For_Portable_Mode()
    {
        var result = _converter.Convert(DeploymentModes.Portable, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("Portable-Loc");
    }

    [Test]
    public void Convert_Should_Return_Installation_Text_For_Setup_Mode()
    {
        var result = _converter.Convert(DeploymentModes.SetupInstallation, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("Installation-Loc");
    }

    [Test]
    public void Convert_Should_Return_MSIX_Text_For_Msix_Mode()
    {
        var result = _converter.Convert(DeploymentModes.MsixInstallation, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be("MSIX-Loc");
    }

    [Test]
    public void Convert_Should_Return_Empty_For_Unsupported_Value_Type()
    {
        var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Test]
    public void ConvertBack_Is_Not_Supported_And_Returns_Null()
    {
        var result = _converter.ConvertBack("any", typeof(DeploymentModes), null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }
}