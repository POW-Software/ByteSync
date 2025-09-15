using System.Reflection;
using System.Runtime.InteropServices;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Services.Applications;

namespace ByteSync.Client.IntegrationTests.Services.Applications;

public class EnvironmentService_Tests
{
    [Test]
    [Platform(Include = "Win")]
    public void SetDeploymentMode_detects_msix_and_parses_pfn_on_windowsapps_path()
    {
        var service = new EnvironmentService();
        
        var containerDir = "POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda";
        var exeFullPath = $"C:\\Program Files\\WindowsApps\\{containerDir}\\ByteSync.exe";
        
        service.Arguments = [exeFullPath];
        
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        Assert.That(service.DeploymentMode, Is.EqualTo(DeploymentModes.MsixInstallation));
        Assert.That(service.MsixPackageFamilyName, Is.EqualTo("POWSoftware.ByteSync_f852479tj7xda"));
    }
    
    [Test]
    public void SetDeploymentMode_marks_setup_when_brew_paths_are_detected()
    {
        var service = new EnvironmentService();
        
        service.Arguments = ["/homebrew/Cellar/bytesync/1.0/bin/bytesync"];
        
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        Assert.That(service.DeploymentMode, Is.EqualTo(DeploymentModes.SetupInstallation));
        
        service.Arguments = ["/linuxbrew/Cellar/bytesync/1.0/bin/bytesync"];
        
        typeof(EnvironmentService)
            .GetMethod("SetDeploymentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, null);
        
        Assert.That(service.DeploymentMode, Is.EqualTo(DeploymentModes.SetupInstallation));
    }
    
    [Test]
    public void OperateCommandLine_true_for_update_and_version()
    {
        var service = new EnvironmentService();
        
        service.Arguments = ["--update"];
        Assert.That(service.OperateCommandLine, Is.True);
        
        service.Arguments = ["--version"];
        Assert.That(service.OperateCommandLine, Is.True);
    }
    
    [Test]
    public void OperateCommandLine_true_for_join_inventory_sync_with_no_gui_only()
    {
        var service = new EnvironmentService();
        
        service.Arguments = ["--join", "--no-gui"];
        Assert.That(service.OperateCommandLine, Is.True);
        
        service.Arguments = ["--inventory", "--no-gui"];
        Assert.That(service.OperateCommandLine, Is.True);
        
        service.Arguments = ["--synchronize", "--no-gui"];
        Assert.That(service.OperateCommandLine, Is.True);
        
        service.Arguments = ["--join"];
        Assert.That(service.OperateCommandLine, Is.False);
    }
    
    [Test]
    public void OSPlatform_maps_current_runtime()
    {
        var service = new EnvironmentService();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.That(service.OSPlatform, Is.EqualTo(OSPlatforms.Windows));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.That(service.OSPlatform, Is.EqualTo(OSPlatforms.Linux));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.That(service.OSPlatform, Is.EqualTo(OSPlatforms.MacOs));
        }
        else
        {
            Assert.That(service.OSPlatform, Is.EqualTo(OSPlatforms.Undefined));
        }
    }
    
    [Test]
    public void OperationMode_and_Auto_flags_follow_arguments()
    {
        var service = new EnvironmentService();
        
        service.Arguments = [];
        Assert.That(service.OperateCommandLine, Is.False);
        Assert.That(service.OperationMode, Is.EqualTo(OperationMode.GraphicalUserInterface));
        Assert.That(service.IsAutoLogin(), Is.False);
        Assert.That(service.IsAutoRunProfile(), Is.False);
        
        service.Arguments = ["--join", "--no-gui"];
        Assert.That(service.OperateCommandLine, Is.True);
        Assert.That(service.OperationMode, Is.EqualTo(OperationMode.CommandLine));
        Assert.That(service.IsAutoLogin(), Is.True);
        Assert.That(service.IsAutoRunProfile(), Is.True);
        
        service.Arguments = ["--synchronize"];
        Assert.That(service.OperateCommandLine, Is.False);
        Assert.That(service.OperationMode, Is.EqualTo(OperationMode.GraphicalUserInterface));
        Assert.That(service.IsAutoLogin(), Is.True);
        Assert.That(service.IsAutoRunProfile(), Is.True);
    }
    
    [Test]
    public void MachineName_uses_override_when_argument_present_otherwise_environment()
    {
        var service = new EnvironmentService();
        
        var expectedEnvMachineName = Environment.MachineName;
        service.Arguments = [];
        Assert.That(service.MachineName, Is.EqualTo(expectedEnvMachineName));
        
        var custom = "TestMachine-XYZ";
        service.Arguments = ["--set-machine-name=" + custom];
        Assert.That(service.MachineName, Is.EqualTo(custom));
    }
    
    [Test]
    public void ApplicationVersion_uses_override_when_argument_present_otherwise_assembly_version()
    {
        var service = new EnvironmentService();
        
        service.Arguments = [];
        var asmVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        Assert.That(service.ApplicationVersion, Is.EqualTo(asmVersion));
        
        var overrideVersion = new Version(3, 2, 1, 0);
        service.Arguments = ["--set-application-version=" + overrideVersion];
        Assert.That(service.ApplicationVersion, Is.EqualTo(overrideVersion));
    }
    
    [Test]
    public void SetClientId_sets_ids_and_instance_contains_guid()
    {
        var service = new EnvironmentService();
        
        var clientId = "client-123";
        service.SetClientId(clientId);
        
        Assert.That(service.ClientId, Is.EqualTo(clientId));
        Assert.That(service.ClientInstanceId, Does.StartWith(clientId + "_"));
        
        var suffix = service.ClientInstanceId.Substring(clientId.Length + 1);
        Assert.That(Guid.TryParse(suffix, out _), Is.True);
    }
}