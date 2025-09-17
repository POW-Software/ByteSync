using System.Globalization;
using System.Reflection;
using Autofac;
using Avalonia.Controls;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services;
using ByteSync.Services.Converters;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Converters;

[TestFixture]
public class DeploymentModeToStringConverterTests
{
    private static readonly PropertyInfo DesignModeProperty =
        typeof(Design).GetProperty("IsDesignMode", BindingFlags.Static | BindingFlags.Public)!;
    
    private Mock<ILocalizationService> _mockLocalizationService = null!;
    private DeploymentModeToStringConverter _converter = null!;
    
    [SetUp]
    public void SetUp()
    {
        SetDesignMode(false);
        
        _mockLocalizationService = new Mock<ILocalizationService>();
        
        _mockLocalizationService.Setup(x => x["DeploymentMode_Portable"])
            .Returns("Portable-Loc");
        _mockLocalizationService.Setup(x => x["DeploymentMode_Installation"])
            .Returns("Installation-Loc");
        _mockLocalizationService.Setup(x => x["DeploymentMode_MSIX"])
            .Returns("MSIX-Loc");
        _mockLocalizationService.Setup(x => x["DeploymentMode_Homebrew"])
            .Returns("Homebrew-Loc");
        
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
    public void Convert_Should_Return_Homebrew_Text_For_Homebrew_Mode()
    {
        var result = _converter.Convert(DeploymentModes.HomebrewInstallation, typeof(string), null, CultureInfo.InvariantCulture);
        
        result.Should().Be("Homebrew-Loc");
    }
    
    [Test]
    public void Convert_Should_Return_Empty_For_Unsupported_Value_Type()
    {
        var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);
        
        result.Should().Be(string.Empty);
    }
    
    [Test]
    public void Convert_Should_Return_Empty_For_Unknown_Deployment_Mode()
    {
        var result = _converter.Convert((DeploymentModes)999, typeof(string), null, CultureInfo.InvariantCulture);
        
        result.Should().Be(string.Empty);
    }
    
    [Test]
    public void Convert_Should_Return_Placeholder_Object_When_Design_Mode()
    {
        using var _ = OverrideDesignMode(true);
        var converter = new DeploymentModeToStringConverter();
        
        var result = converter.Convert(DeploymentModes.Portable, typeof(string), null, CultureInfo.InvariantCulture);
        
        result.Should().BeOfType<object>();
        result.Should().NotBeOfType<string>();
    }
    
    [Test]
    public void Convert_Should_Resolve_Localization_From_Container_When_Using_Default_Constructor()
    {
        using var _ = OverrideContainer(_mockLocalizationService.Object);
        var converter = new DeploymentModeToStringConverter();
        
        var result = converter.Convert(DeploymentModes.SetupInstallation, typeof(string), null, CultureInfo.InvariantCulture);
        
        result.Should().Be("Installation-Loc");
    }
    
    [Test]
    public void ConvertBack_Is_Not_Supported_And_Returns_Null()
    {
        var result = _converter.ConvertBack("any", typeof(DeploymentModes), null, CultureInfo.InvariantCulture);
        
        result.Should().BeNull();
    }
    
    private static IDisposable OverrideDesignMode(bool isDesignMode)
    {
        var original = GetDesignMode();
        SetDesignMode(isDesignMode);
        
        return new RevertAction(() => SetDesignMode(original));
    }
    
    private static IDisposable OverrideContainer(ILocalizationService localizationService)
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(localizationService).As<ILocalizationService>();
        var container = builder.Build();
        
        ContainerProvider.Container = container;
        
        return new RevertAction(() =>
        {
            container.Dispose();
            ContainerProvider.Container = null!;
        });
    }
    
    private static bool GetDesignMode()
    {
        return (bool)DesignModeProperty.GetValue(null)!;
    }
    
    private static void SetDesignMode(bool value)
    {
        DesignModeProperty.SetValue(null, value);
    }
    
    private sealed class RevertAction : IDisposable
    {
        private readonly Action _onDispose;
        
        public RevertAction(Action onDispose)
        {
            _onDispose = onDispose;
        }
        
        public void Dispose()
        {
            _onDispose();
        }
    }
}