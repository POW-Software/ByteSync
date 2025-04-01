using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ByteSync.Business.Arguments;
using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;

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
        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            // https://stackoverflow.com/questions/3820613/where-the-application-should-store-its-logs-in-mac-os
            // Beaucoup d'application sont dans Environment.SpecialFolder.LocalApplicationData
                
            var globalApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            thisApplicationDataPath = IOUtils.Combine(globalApplicationDataPath, "POW Software", "ByteSync");
        }
        else
        {
                
            if (_environmentService.IsPortableApplication)
            {
                var fileInfo = new FileInfo(_environmentService.AssemblyFullName);
                var parent = fileInfo.Directory!;
                
                thisApplicationDataPath = IOUtils.Combine(parent.FullName, "ApplicationData");
            }
            else
            {
                string globalApplicationDataPath;
                if (IOUtils.IsSubPathOf(_environmentService.AssemblyFullName, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
                {
                    // On est installé dans Roaming
                    globalApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }
                else
                {
                    // On utilise Local
                    globalApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                }
                    
                thisApplicationDataPath = IOUtils.Combine(globalApplicationDataPath, "POW Software", "ByteSync");
            }
        }

        if (_environmentService.ExecutionMode == ExecutionMode.Debug)
        {
            if (Environment.GetCommandLineArgs().Any(a => a.StartsWith(DebugArguments.LADM_USE_APPDATA)))
            {
                var arg = Environment.GetCommandLineArgs()
                    .First(a => a.StartsWith(DebugArguments.LADM_USE_APPDATA))
                    .Substring(DebugArguments.LADM_USE_APPDATA.Length);
                
                thisApplicationDataPath += $" {arg}";
            }
            else if (!Environment.GetCommandLineArgs().Contains(DebugArguments.LADM_USE_STANDARD_APPDATA))
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