using System.Reflection;
using System.Runtime.InteropServices;
using ByteSync.Business.Arguments;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Services.Applications;

public class EnvironmentService : IEnvironmentService
{
    public EnvironmentService()
    {
        Arguments = Environment.GetCommandLineArgs();
        
        // ReSharper disable once RedundantAssignment
        bool isDebug = false;
    #if DEBUG
        isDebug = true;
    #endif

        ExecutionMode = isDebug ? ExecutionMode.Debug : ExecutionMode.Regular;

        SetAssemblyFullName();
        SetIsPortableApplication();
    }
    
    public string ClientId { get; private set; }
    
    public string ClientInstanceId { get; private set; }
    
    private void SetAssemblyFullName()
    {
        AssemblyFullName = Environment.GetCommandLineArgs()[0];
    }
    
    private void SetIsPortableApplication()
    {
        var applicationLauncherFullName = Environment.GetCommandLineArgs()[0].ToLower();

        var programsDirectoriresCandidates = BuildProgramsDirectoriresCandidates(
            Environment.SpecialFolder.CommonProgramFiles,
            Environment.SpecialFolder.CommonProgramFilesX86,
            Environment.SpecialFolder.ProgramFiles,
            Environment.SpecialFolder.ProgramFilesX86,
            Environment.SpecialFolder.LocalApplicationData, // Local
            Environment.SpecialFolder.ApplicationData, // Roaming
            Environment.SpecialFolder.CommonApplicationData); // ProgramData

        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            programsDirectoriresCandidates.Add("/usr/bin");
            programsDirectoriresCandidates.Add("/bin");
            programsDirectoriresCandidates.Add("/usr/local/bin");
            programsDirectoriresCandidates.Add("/usr/share/");
        }

        if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            programsDirectoriresCandidates.Add("/Applications");
        }

        bool isPortableApplication = true;
        foreach (var candidate in programsDirectoriresCandidates)
        {
            if (IOUtils.IsSubPathOf(applicationLauncherFullName, candidate))
            {
                isPortableApplication = false;
            }
        }
            
        IsPortableApplication = isPortableApplication;
    }
    
    private HashSet<string> BuildProgramsDirectoriresCandidates(params Environment.SpecialFolder[] specialFolders)
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
            bool operateCommandLine = false;
            
            if (Arguments.Contains(RegularArguments.UPDATE))
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
        get
        {
            return OperateCommandLine ? OperationMode.CommandLine : OperationMode.GraphicalUserInterface;
        }
    }
    
    public bool IsAutoLogin()
    {
        bool isAutoLogin = Arguments.Contains(RegularArguments.JOIN) || Arguments.Contains(RegularArguments.INVENTORY)
                                                                     || Arguments.Contains(RegularArguments.SYNCHRONIZE);

        return isAutoLogin;
    }

    public bool IsAutoRunProfile()
    {
        return IsAutoLogin();
    }
    
    public bool IsPortableApplication { get; private set; } = false;
        
    public string AssemblyFullName { get; private set; }  = null!;

    public string MachineName
    {
        get
        {
            string machineName;
                
            if (Environment.GetCommandLineArgs().Any(a => a.StartsWith(RegularArguments.SET_MACHINE_NAME)))
            {
                machineName = Environment.GetCommandLineArgs()
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
    
    public Version CurrentVersion
    {
        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version!;
        }
    }

    public void SetClientId(string clientId)
    {
        ClientId = clientId;
        ClientInstanceId = $"{ClientId}_{Guid.NewGuid():D}";
    }
}