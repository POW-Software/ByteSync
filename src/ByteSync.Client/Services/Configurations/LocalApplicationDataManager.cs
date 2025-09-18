using System.Diagnostics;
using System.IO;
using ByteSync.Business.Arguments;
using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;

// using System.Runtime.InteropServices;

namespace ByteSync.Services.Configurations;

public class LocalApplicationDataManager : ILocalApplicationDataManager
{
    private readonly IEnvironmentService _environmentService;
    
    public LocalApplicationDataManager(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
        
        Initialize();
    }
    
    public string ApplicationDataPath { get; private set; } = null!;
    
    private void Initialize()
    {
        string thisApplicationDataPath;
        if (_environmentService.OSPlatform == OSPlatforms.MacOs)
        {
            var globalApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            thisApplicationDataPath = IOUtils.Combine(globalApplicationDataPath, "POW Software", "ByteSync");
        }
        else
        {
            switch (_environmentService.DeploymentMode)
            {
                case DeploymentModes.Portable:
                {
                    var fileInfo = new FileInfo(_environmentService.AssemblyFullName);
                    var parent = fileInfo.Directory!;
                    thisApplicationDataPath = IOUtils.Combine(parent.FullName, "ApplicationData");
                    
                    break;
                }
                case DeploymentModes.MsixInstallation:
                {
                    var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var pfn = _environmentService.MsixPackageFamilyName ?? "";
                    var physicalRoot = IOUtils.Combine(local, "Packages", pfn, "LocalCache", "Local");
                    thisApplicationDataPath = IOUtils.Combine(physicalRoot, "POW Software", "ByteSync");
                    
                    break;
                }
                default:
                {
                    string globalApplicationDataPath;
                    if (IOUtils.IsSubPathOf(_environmentService.AssemblyFullName,
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
                    {
                        // Installed in Roaming
                        globalApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    }
                    else
                    {
                        // We use Local
                        globalApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    }
                    
                    thisApplicationDataPath = IOUtils.Combine(globalApplicationDataPath, "POW Software", "ByteSync");
                    
                    break;
                }
            }
        }
        
        if (_environmentService.ExecutionMode == ExecutionMode.Debug)
        {
            if (_environmentService.Arguments.Any(a => a.StartsWith(DebugArguments.LADM_USE_APPDATA)))
            {
                var arg = Environment.GetCommandLineArgs()
                    .First(a => a.StartsWith(DebugArguments.LADM_USE_APPDATA))
                    .Substring(DebugArguments.LADM_USE_APPDATA.Length);
                
                thisApplicationDataPath += $" {arg}";
            }
            else if (!_environmentService.Arguments.Contains(DebugArguments.LADM_USE_STANDARD_APPDATA))
            {
                thisApplicationDataPath += " Debug";
                
                var debugPath = DateTime.Now.ToString("yy-MM-dd HH-mm") + "_" + Process.GetCurrentProcess().Id;
                thisApplicationDataPath = IOUtils.Combine(thisApplicationDataPath, debugPath);
            }
        }
        
        if (!Directory.Exists(thisApplicationDataPath))
        {
            Directory.CreateDirectory(thisApplicationDataPath);
        }
        
        ApplicationDataPath = thisApplicationDataPath;
    }
    
    public string? LogFilePath
    {
        get
        {
            // https://stackoverflow.com/questions/39973928/serilog-open-log-file
            
            var directoryInfo = new DirectoryInfo(
                IOUtils.Combine(ApplicationDataPath, LocalApplicationDataConstants.LOGS_DIRECTORY));
            
            var files = directoryInfo.GetFiles("ByteSync_*.log", SearchOption.TopDirectoryOnly);
            
            var currentLogFilePath = files
                .Where(f => !f.Name.Contains("_debug"))
                .MaxBy(f => f.LastWriteTime)?.FullName;
            
            return currentLogFilePath;
        }
    }
    
    public string? DebugLogFilePath
    {
        get
        {
            // https://stackoverflow.com/questions/39973928/serilog-open-log-file
            
            var directoryInfo = new DirectoryInfo(
                IOUtils.Combine(ApplicationDataPath, LocalApplicationDataConstants.LOGS_DIRECTORY));
            
            var files = directoryInfo.GetFiles("ByteSync_*.log", SearchOption.TopDirectoryOnly);
            
            var currentLogFilePath = files
                .Where(f => f.Name.Contains("_debug"))
                .MaxBy(f => f.LastWriteTime)?.FullName;
            
            return currentLogFilePath;
        }
    }
}