using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ByteSync.Business.Arguments;
using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
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

    public string ShellApplicationDataPath { get; private set; } = null!;

    private void Initialize()
    {
        string thisApplicationDataPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // https://stackoverflow.com/questions/3820613/where-the-application-should-store-its-logs-in-mac-os
            // Many applications are in Environment.SpecialFolder.LocalApplicationData

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

        ShellApplicationDataPath = ComputeShellApplicationDataPath();
    }

    private string ComputeShellApplicationDataPath()
    {
        var shellPath = ApplicationDataPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var assembly = _environmentService.AssemblyFullName;
            if (assembly.Contains("\\Program Files\\WindowsApps\\") ||
                assembly.Contains("\\Program Files (x86)\\WindowsApps\\"))
            {
                var exeDirectory = new FileInfo(assembly).Directory;
                var containerDirName = exeDirectory!.Name;

                var idxUnderscore = containerDirName.IndexOf('_');
                var idxDoubleUnderscore = containerDirName.IndexOf("__", StringComparison.Ordinal);
                if (idxUnderscore > 0 && idxDoubleUnderscore > idxUnderscore)
                {
                    var name = containerDirName.Substring(0, idxUnderscore);
                    var publisherId = containerDirName.Substring(idxDoubleUnderscore + 2);
                    var pfn = $"{name}_{publisherId}";

                    var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var physicalRoot = IOUtils.Combine(local, "Packages", pfn, "LocalCache", "Local");

                    var logicalRoot = IOUtils.Combine(local, "POW Software", "ByteSync");

                    if (IOUtils.IsSubPathOf(ApplicationDataPath, logicalRoot))
                    {
                        var relative = ApplicationDataPath.Substring(logicalRoot.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        shellPath = IOUtils.Combine(physicalRoot, "POW Software", "ByteSync", relative);
                    }
                    else
                    {
                        shellPath = IOUtils.Combine(physicalRoot, "POW Software", "ByteSync");
                    }
                }
            }
        }

        return shellPath;
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

    public string GetShellPath(string path)
    {
        if (ShellApplicationDataPath == ApplicationDataPath)
        {
            return path;
        }

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logicalRoot = IOUtils.Combine(local, "POW Software", "ByteSync");
        if (IOUtils.IsSubPathOf(path, logicalRoot))
        {
            var relative = path.Substring(logicalRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return IOUtils.Combine(ShellApplicationDataPath, relative);
        }

        return path;
    }
}