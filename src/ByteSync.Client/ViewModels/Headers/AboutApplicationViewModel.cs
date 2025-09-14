using System.Reactive;
using System.Runtime.InteropServices;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Services.Misc;
using ByteSync.ViewModels.Misc;
using ReactiveUI;

namespace ByteSync.ViewModels.Headers;

public class AboutApplicationViewModel : FlyoutElementViewModel
{
    private readonly IEnvironmentService _environmentService;
    private readonly IWebAccessor _webAccessor;
    private readonly IFileSystemAccessor _fileSystemAccessor;
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly ILogger<AboutApplicationViewModel> _logger;

    public AboutApplicationViewModel()
    {
    }

    public AboutApplicationViewModel(IEnvironmentService environmentService, IWebAccessor webAccessor,
        IFileSystemAccessor fileSystemAccessor,
        ILocalApplicationDataManager localApplicationDataManager, ILogger<AboutApplicationViewModel> logger)
    {
        _environmentService = environmentService;
        _webAccessor = webAccessor;
        _fileSystemAccessor = fileSystemAccessor;
        _localApplicationDataManager = localApplicationDataManager;
        _logger = logger;

        VisitByteSyncWebSiteCommand = ReactiveCommand.CreateFromTask(VisitByteSyncWebSite);
        VisitByteSyncWebSiteCommand.ThrownExceptions.Subscribe(OnCommandException);

        VisitByteSyncRepositoryCommand = ReactiveCommand.CreateFromTask(VisitByteSyncRepository);
        VisitByteSyncRepositoryCommand.ThrownExceptions.Subscribe(OnCommandException);

        VisitPowSoftwareWebSiteCommand = ReactiveCommand.CreateFromTask(VisitPowSoftwareWebSite);
        VisitPowSoftwareWebSiteCommand.ThrownExceptions.Subscribe(OnCommandException);

        ExploreAppDataCommand = ReactiveCommand.CreateFromTask(ExploreAppData);
        ExploreAppDataCommand.ThrownExceptions.Subscribe(OnCommandException);

        OpenLogCommand = ReactiveCommand.CreateFromTask(OpenLogAsync);
        OpenLogCommand.ThrownExceptions.Subscribe(OnCommandException);
    }

    public ReactiveCommand<Unit, Unit> ExploreAppDataCommand { get; }

    public ReactiveCommand<Unit, Unit> VisitByteSyncWebSiteCommand { get; }

    public ReactiveCommand<Unit, Unit> VisitByteSyncRepositoryCommand { get; }

    public ReactiveCommand<Unit, Unit> VisitPowSoftwareWebSiteCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenLogCommand { get; }

    public string ApplicationVersion => VersionHelper.GetVersionString(_environmentService.ApplicationVersion);

    public bool DeploymentMode => _environmentService.IsPortableApplication;

    public string ClientId => _environmentService.ClientId;

    public string ClientInstanceId => _environmentService.ClientInstanceId;

    public string MachineName => _environmentService.MachineName;

    public string OperatingSystem => $"{RuntimeInformation.OSDescription}{(Environment.Is64BitOperatingSystem ? " (64 bits)" : "")}";

    private async Task VisitByteSyncWebSite()
    {
        await _webAccessor.OpenByteSyncWebSite();
    }

    private async Task VisitByteSyncRepository()
    {
        await _webAccessor.OpenByteSyncRepository();
    }

    private async Task VisitPowSoftwareWebSite()
    {
        await _webAccessor.OpenPowSoftwareWebSite();
    }

    private async Task ExploreAppData()
    {
        await _fileSystemAccessor.OpenDirectory(_localApplicationDataManager.ShellApplicationDataPath);
    }

    private async Task OpenLogAsync()
    {
        var logFilePath = _localApplicationDataManager.DebugLogFilePath;

        if (logFilePath.IsNullOrEmpty(true))
        {
            logFilePath = _localApplicationDataManager.LogFilePath;
        }

        if (logFilePath != null)
        {
            var shellPath = _localApplicationDataManager.GetShellPath(logFilePath);
            await _fileSystemAccessor.OpenFile(shellPath);
        }
        else
        {
            _logger.LogError("GeneralSettingsViewModel.OpenLogAsync: Unable to find log file path");
        }
    }

    private void OnCommandException(Exception exception)
    {
        _logger.LogError(exception, "An error has occured");
    }
}