using System.Runtime.InteropServices;
using ByteSync.Business.Arguments;
using ByteSync.Business.Misc;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;

namespace ByteSync.Services.Bootstrappers;

public class BootstrapLogger : IBootstrapLogger
{
    private readonly ILogger<BootstrapLogger> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly ILocalApplicationDataManager _localApplicationDataManager;

    public BootstrapLogger(ILogger<BootstrapLogger> logger, IEnvironmentService environmentService, ILocalApplicationDataManager localApplicationDataManager)
    {
        _logger = logger;
        _environmentService = environmentService;
        _localApplicationDataManager = localApplicationDataManager;
    }

    public void LogBootstrapHeader()
    {
        _logger.LogInformation($"*************************************************");
        _logger.LogInformation($"***     ByteSync - Application Startup       ****");
        _logger.LogInformation($"*************************************************");
        _logger.LogInformation($"Version: {_environmentService.ApplicationVersion}");
        _logger.LogInformation($"MachineName: {_environmentService.MachineName}");
        _logger.LogInformation($"OS: {RuntimeInformation.OSDescription}{(Environment.Is64BitOperatingSystem ? " (64 bits)" : "")}");
        _logger.LogInformation($"AssemblyFullName: {_environmentService.AssemblyFullName}");
        _logger.LogInformation($"DeploymentMode: {(_environmentService.IsPortableApplication ? "Portable" : "Installed")}");
        _logger.LogInformation($"*************************************************");

        if (_environmentService.ExecutionMode == ExecutionMode.Debug)
        {
            _logger.LogInformation(" | Running in DEBUG Mode |");
            _logger.LogInformation($"*************************************************");
        }
    }

    public void LogBootstrapContent()
    {
        _logger.LogInformation("Command Line Arguments:");
        for (int i = 0; i < _environmentService.Arguments.Length; i++)
        {
            _logger.LogInformation(" - Argument {i}: {arg}", i + 1, _environmentService.Arguments[i]);
        }

        _logger.LogInformation($"*************************************************");
        
        if (_environmentService.Arguments.Contains(RegularArguments.LOG_DEBUG))
        {
            _logger.LogInformation("LoggingLevel: Debug ({Arg})", RegularArguments.LOG_DEBUG);
            _logger.LogInformation($"*************************************************");
        }

        _logger.LogInformation("ApplicationDataPath: '{applicationDataPath}'", _localApplicationDataManager.ApplicationDataPath);

        LogSpecialFolders();
    }
    
    private void LogSpecialFolders()
    {
        string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        string commonProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        string commonProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _logger.LogDebug("programs: '{programs}'", programs);
        _logger.LogDebug("cprogramFiles: '{cprogramFiles}'", commonProgramFiles);
        _logger.LogDebug("cprogramFilesX86: '{cprogramFilesX86}'", commonProgramFilesX86);
        _logger.LogDebug("programFiles: '{programFiles}'", programFiles);
        _logger.LogDebug("programFilesX86: '{programFilesX86}'", programFilesX86);
        _logger.LogDebug("globalApplicationDataPath: '{globalApplicationDataPath}'", applicationData);
        _logger.LogDebug("commonApplicationDataPath: '{commonApplicationDataPath}'", commonApplicationData);
    }
}