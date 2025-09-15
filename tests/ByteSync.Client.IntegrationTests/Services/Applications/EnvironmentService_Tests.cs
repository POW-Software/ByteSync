using System.Reflection;
using System.Runtime.InteropServices;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Services.Applications;
using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Services.Applications;

public class EnvironmentService_Tests
{
    [Test]
    [Platform(Include = "Win")]
    public void SetDeploymentMode_detects_msix_and_parses_pfn_on_windowsapps_path()
    {
        // Arrange
        var service = new EnvironmentService();
        var containerDir = "POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda";
        var exeFullPath = $"C:\\Program Files\\WindowsApps\\{containerDir}\\ByteSync.exe";
        
        service.Arguments = [exeFullPath];
        
        // Act
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        // Assert
        service.DeploymentMode.Should().Be(DeploymentModes.MsixInstallation);
        service.MsixPackageFamilyName.Should().Be("POWSoftware.ByteSync_f852479tj7xda");
    }
    
    [Test]
    [Platform(Include = "Win")]
    public void SetDeploymentMode_detects_msix_even_when_pfn_parse_fails_and_keeps_null_pfn()
    {
        // Arrange - container folder without expected "__" separator
        var service = new EnvironmentService();
        var containerDir = "SomeApp_1.0.0.0_neutral_x64";
        var exeFullPath = $"C:\\Program Files\\WindowsApps\\{containerDir}\\SomeApp.exe";
        
        service.Arguments = [exeFullPath];
        
        // Act
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        // Assert
        service.DeploymentMode.Should().Be(DeploymentModes.MsixInstallation);
        service.MsixPackageFamilyName.Should().BeNull();
    }
    
    [Test]
    [Platform(Include = "Win")]
    public void SetDeploymentMode_detects_msix_from_x86_windowsapps_path()
    {
        // Arrange
        var service = new EnvironmentService();
        var containerDir = "Vendor.Product_2.0.0.0_neutral__abc123xyz";
        var exeFullPath = $"C:\\Program Files (x86)\\WindowsApps\\{containerDir}\\Product.exe";
        
        service.Arguments = [exeFullPath];
        
        // Act
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        // Assert
        service.DeploymentMode.Should().Be(DeploymentModes.MsixInstallation);
        service.MsixPackageFamilyName.Should().Be("Vendor.Product_abc123xyz");
    }
    
    [Test]
    public void SetDeploymentMode_marks_setup_when_brew_paths_are_detected()
    {
        var service = new EnvironmentService();
        
        service.Arguments = ["/homebrew/Cellar/bytesync/1.0/bin/bytesync"];
        
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        service.DeploymentMode.Should().Be(DeploymentModes.SetupInstallation);
        
        service.Arguments = ["/linuxbrew/Cellar/bytesync/1.0/bin/bytesync"];
        
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        service.DeploymentMode.Should().Be(DeploymentModes.SetupInstallation);
    }
    
    [Test]
    public void OperateCommandLine_true_for_update_and_version()
    {
        var service = new EnvironmentService();
        
        service.Arguments = ["--update"];
        service.OperateCommandLine.Should().BeTrue();
        
        service.Arguments = ["--version"];
        service.OperateCommandLine.Should().BeTrue();
    }
    
    [Test]
    public void OperateCommandLine_true_for_join_inventory_sync_with_no_gui_only()
    {
        var service = new EnvironmentService();
        
        service.Arguments = ["--join", "--no-gui"];
        service.OperateCommandLine.Should().BeTrue();
        
        service.Arguments = ["--inventory", "--no-gui"];
        service.OperateCommandLine.Should().BeTrue();
        
        service.Arguments = ["--synchronize", "--no-gui"];
        service.OperateCommandLine.Should().BeTrue();
        
        service.Arguments = ["--join"];
        service.OperateCommandLine.Should().BeFalse();
    }
    
    [Test]
    public void OSPlatform_maps_current_runtime()
    {
        var service = new EnvironmentService();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            service.OSPlatform.Should().Be(OSPlatforms.Windows);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            service.OSPlatform.Should().Be(OSPlatforms.Linux);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            service.OSPlatform.Should().Be(OSPlatforms.MacOs);
        }
        else
        {
            service.OSPlatform.Should().Be(OSPlatforms.Undefined);
        }
    }
    
    [Test]
    public void OperationMode_and_Auto_flags_follow_arguments()
    {
        var service = new EnvironmentService();
        
        service.Arguments = [];
        service.OperateCommandLine.Should().BeFalse();
        service.OperationMode.Should().Be(OperationMode.GraphicalUserInterface);
        service.IsAutoLogin().Should().BeFalse();
        service.IsAutoRunProfile().Should().BeFalse();
        
        service.Arguments = ["--join", "--no-gui"];
        service.OperateCommandLine.Should().BeTrue();
        service.OperationMode.Should().Be(OperationMode.CommandLine);
        service.IsAutoLogin().Should().BeTrue();
        service.IsAutoRunProfile().Should().BeTrue();
        
        service.Arguments = ["--synchronize"];
        service.OperateCommandLine.Should().BeFalse();
        service.OperationMode.Should().Be(OperationMode.GraphicalUserInterface);
        service.IsAutoLogin().Should().BeTrue();
        service.IsAutoRunProfile().Should().BeTrue();
    }
    
    [Test]
    public void MachineName_uses_override_when_argument_present_otherwise_environment()
    {
        var service = new EnvironmentService();
        
        var expectedEnvMachineName = Environment.MachineName;
        service.Arguments = [];
        service.MachineName.Should().Be(expectedEnvMachineName);
        
        var custom = "TestMachine-XYZ";
        service.Arguments = ["--set-machine-name=" + custom];
        service.MachineName.Should().Be(custom);
    }
    
    [Test]
    public void ApplicationVersion_uses_override_when_argument_present_otherwise_assembly_version()
    {
        var service = new EnvironmentService();
        
        service.Arguments = [];
        var asmVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        service.ApplicationVersion.Should().Be(asmVersion);
        
        var overrideVersion = new Version(3, 2, 1, 0);
        service.Arguments = ["--set-application-version=" + overrideVersion];
        service.ApplicationVersion.Should().Be(overrideVersion);
    }
    
    [Test]
    public void SetClientId_sets_ids_and_instance_contains_guid()
    {
        var service = new EnvironmentService();
        
        var clientId = "client-123";
        service.SetClientId(clientId);
        
        service.ClientId.Should().Be(clientId);
        service.ClientInstanceId.Should().StartWith(clientId + "_");
        
        var suffix = service.ClientInstanceId.Substring(clientId.Length + 1);
        Guid.TryParse(suffix, out _).Should().BeTrue();
    }
}