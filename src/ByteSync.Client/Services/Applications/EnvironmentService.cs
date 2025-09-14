using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ByteSync.Business.Arguments;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Services.Applications;

public class EnvironmentService : IEnvironmentService
{
    public EnvironmentService()
    {
        Arguments = Environment.GetCommandLineArgs();

        // ReSharper disable once RedundantAssignment
        var isDebug = false;
    #if DEBUG
        isDebug = true;
    #endif

        ExecutionMode = isDebug ? ExecutionMode.Debug : ExecutionMode.Regular;

        SetAssemblyFullName();
        SetDeploymentMode();
    }

    public string ClientId { get; private set; } = null!;

    public string ClientInstanceId { get; private set; } = null!;

    private void SetAssemblyFullName()
    {
        AssemblyFullName = Arguments[0];
    }

    public DeploymentModes DeploymentMode { get; private set; }

    public string? MsixPackageFamilyName { get; private set; }

    private void SetDeploymentMode()
    {
        var applicationLauncherFullName = Arguments[0];

        var programsDirectoriesCandidates = BuildProgramsDirectoriesCandidates(
            Environment.SpecialFolder.CommonProgramFiles,
            Environment.SpecialFolder.CommonProgramFilesX86,
            Environment.SpecialFolder.ProgramFiles,
            Environment.SpecialFolder.ProgramFilesX86,
            Environment.SpecialFolder.LocalApplicationData, // Local
            Environment.SpecialFolder.ApplicationData, // Roaming
            Environment.SpecialFolder.CommonApplicationData); // ProgramData

        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            programsDirectoriesCandidates.Add("/usr/bin");
            programsDirectoriesCandidates.Add("/bin");
            programsDirectoriesCandidates.Add("/usr/local/bin");
            programsDirectoriesCandidates.Add("/usr/share/");
        }

        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            programsDirectoriesCandidates.Add("/Applications");

            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            programsDirectoriesCandidates.Add($"{homeDirectory}/Applications");
        }

        // Detect MSIX on Windows by install path
        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            if (applicationLauncherFullName.Contains("\\Program Files\\WindowsApps\\", StringComparison.OrdinalIgnoreCase) ||
                applicationLauncherFullName.Contains("\\Program Files (x86)\\WindowsApps\\", StringComparison.OrdinalIgnoreCase))
            {
                // Parse PFN from container directory name
                var exeDirectory = new FileInfo(applicationLauncherFullName).Directory;
                var containerDirName = exeDirectory!.Name; // e.g. POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda

                var idxUnderscore = containerDirName.IndexOf('_');
                var idxDoubleUnderscore = containerDirName.IndexOf("__", StringComparison.Ordinal);
                if (idxUnderscore > 0 && idxDoubleUnderscore > idxUnderscore)
                {
                    var name = containerDirName.Substring(0, idxUnderscore);
                    var publisherId = containerDirName.Substring(idxDoubleUnderscore + 2);
                    MsixPackageFamilyName = $"{name}_{publisherId}";
                }

                DeploymentMode = DeploymentModes.MsixInstallation;
                IsPortableApplication = false;

                return;
            }
        }

        var installedInPrograms = false;
        foreach (var candidate in programsDirectoriesCandidates)
        {
            if (IOUtils.IsSubPathOf(applicationLauncherFullName, candidate))
            {
                installedInPrograms = true;

                break;
            }
        }

        if (applicationLauncherFullName.Contains("/homebrew/", StringComparison.OrdinalIgnoreCase) ||
            applicationLauncherFullName.Contains("/linuxbrew/", StringComparison.OrdinalIgnoreCase))
        {
            installedInPrograms = true;
        }

        DeploymentMode = installedInPrograms ? DeploymentModes.SetupInstallation : DeploymentModes.Portable;
        IsPortableApplication = DeploymentMode == DeploymentModes.Portable;
    }

    private HashSet<string> BuildProgramsDirectoriesCandidates(params Environment.SpecialFolder[] specialFolders)
    {
        var result = new HashSet<string>();
        foreach (var specialFolder in specialFolders)
        {
            var fullName = Environment.GetFolderPath(specialFolder);

            if (fullName.IsNotEmpty() && fullName.Length > 1)
            {
                result.Add(fullName);
            }
        }

        return result;
    }

    public ExecutionMode ExecutionMode { get; }

    public string[] Arguments { get; set; }

    public bool OperateCommandLine
    {
        get
        {
            var operateCommandLine = false;

            if (Arguments.Contains(RegularArguments.UPDATE) || Arguments.Contains(RegularArguments.VERSION))
            {
                operateCommandLine = true;
            }
            else if ((Arguments.Contains(RegularArguments.JOIN) || Arguments.Contains(RegularArguments.INVENTORY)
                                                                || Arguments.Contains(RegularArguments.SYNCHRONIZE))
                     && Arguments.Contains(RegularArguments.NO_GUI))
            {
                operateCommandLine = true;
            }

            return operateCommandLine;
        }
    }

    public OSPlatforms OSPlatform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return OSPlatforms.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                return OSPlatforms.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                return OSPlatforms.MacOs;
            }

            return OSPlatforms.Undefined;
        }
    }

    public OperationMode OperationMode
    {
        get { return OperateCommandLine ? OperationMode.CommandLine : OperationMode.GraphicalUserInterface; }
    }

    public bool IsAutoLogin()
    {
        var isAutoLogin = Arguments.Contains(RegularArguments.JOIN) || Arguments.Contains(RegularArguments.INVENTORY)
                                                                    || Arguments.Contains(RegularArguments.SYNCHRONIZE);

        return isAutoLogin;
    }

    public bool IsAutoRunProfile()
    {
        return IsAutoLogin();
    }

    public bool IsPortableApplication { get; private set; }

    public string AssemblyFullName { get; private set; } = null!;

    public string MachineName
    {
        get
        {
            string machineName;

            if (Arguments.Any(a => a.StartsWith(RegularArguments.SET_MACHINE_NAME)))
            {
                machineName = Arguments
                    .First(a => a.StartsWith(RegularArguments.SET_MACHINE_NAME))
                    .Substring(RegularArguments.SET_MACHINE_NAME.Length);
            }
            else
            {
                machineName = Environment.MachineName;
            }

            return machineName;
        }
    }

    public Version ApplicationVersion
    {
        get
        {
            if (Arguments.Any(a => a.StartsWith(DebugArguments.SET_APPLICATION_VERSION)))
            {
                var versionString = Arguments
                    .First(a => a.StartsWith(DebugArguments.SET_APPLICATION_VERSION))
                    .Substring(DebugArguments.SET_APPLICATION_VERSION.Length);

                return new Version(versionString);
            }

            return Assembly.GetExecutingAssembly().GetName().Version!;
        }
    }

    public void SetClientId(string clientId)
    {
        ClientId = clientId;
        ClientInstanceId = $"{ClientId}_{Guid.NewGuid():D}";
    }
}