using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Arguments;
using ByteSync.Business.Updates;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;
using ByteSync.Services.Misc;

namespace ByteSync.Services.Updates;

public class ApplyUpdateService : IApplyUpdateService
{
    private readonly IEnvironmentService _environmentService;
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly IUpdateRepository _updateRepository;
    private readonly IUpdateHelperService _updateHelperService;
    private readonly IUpdateDownloader _updateDownloader;
    private readonly IApplicationRestarter _applicationRestarter;
    private readonly IUpdateExistingFilesBackuper _updateExistingFilesBackuper;
    private readonly IUpdateNewFilesMover _updateNewFilesMover;
    private readonly IUpdateExtractor _updateExtractor;
    private readonly ILogger<ApplyUpdateService> _logger;

    public ApplyUpdateService(IEnvironmentService environmentService, ILocalApplicationDataManager localApplicationDataManager, 
        IUpdateRepository updateRepository, IUpdateHelperService updateHelperService, IUpdateDownloader updateDownloader,
        IApplicationRestarter applicationRestarter, IUpdateExistingFilesBackuper updateExistingFilesBackuper, 
        IUpdateNewFilesMover updateNewFilesMover, IUpdateExtractor updateExtractor, ILogger<ApplyUpdateService> logger)
    {
        _environmentService = environmentService;
        _localApplicationDataManager = localApplicationDataManager;
        _updateRepository = updateRepository;
        _updateHelperService = updateHelperService;
        _updateDownloader = updateDownloader;
        _applicationRestarter = applicationRestarter;
        _updateExistingFilesBackuper = updateExistingFilesBackuper;
        _updateNewFilesMover = updateNewFilesMover;
        _updateExtractor = updateExtractor;
        _logger = logger;
    }

    public string ApplicationLauncherFullName { get; private set; }

    public async Task<bool> Update(SoftwareVersion softwareVersion, SoftwareVersionFile softwareVersionFile, CancellationToken cancellationToken)
    {
        _updateRepository.UpdateData = new UpdateData { SoftwareVersionFile = softwareVersionFile };
            
        _logger.LogInformation("UpdateApplier: Starting update");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (_environmentService.AssemblyFullName.StartsWith("/private/var/folders/") &&
                _environmentService.AssemblyFullName.Contains("/AppTranslocation/"))
            {
                throw new Exception("Can not auto-update translocated application on MacOS");
            }
        }
        
        ApplicationLauncherFullName = _applicationRestarter.ApplicationLauncherFullName;

        ComputeApplicationBaseDirectory();
        ComputeDownloadLocation();
        ComputeUnzipLocation();

        var currentVersion = VersionHelper.GetVersionString(_environmentService.ApplicationVersion);

        _logger.LogInformation("ApplyUpdateService: Current Version: {CurrentVersion}, Update version:{Version}", currentVersion, softwareVersion.Version);
        _logger.LogInformation("ApplyUpdateService: ApplicationLauncherFullName:{ApplicationLauncherFullName}", ApplicationLauncherFullName);
        _logger.LogInformation("ApplyUpdateService: ApplicationBaseDirectory:{ApplicationBaseDirectory}", _updateRepository.UpdateData.ApplicationBaseDirectory);
        _logger.LogInformation("ApplyUpdateService: FileToDownload:{FileToDownload}", _updateRepository.UpdateData.FileToDownload);
        _logger.LogInformation("ApplyUpdateService: Platform:{Platform}", softwareVersionFile.Platform);
        _logger.LogInformation("ApplyUpdateService: Level:{Level}", softwareVersion.Level);
        _logger.LogInformation("ApplyUpdateService: DownloadLocation:{DownloadLocation}", _updateRepository.UpdateData.DownloadLocation);
        _logger.LogInformation("ApplyUpdateService: UnzipLocation:{UnzipLocation}", _updateRepository.UpdateData.UnzipLocation);
        
        ReportProgress(new UpdateProgress(UpdateProgressStatus.Downloading, 0));
        await _updateDownloader.DownloadAsync(cancellationToken); 
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        
        ReportProgress(new UpdateProgress(UpdateProgressStatus.Extracting));
        await _updateExtractor.ExtractAsync();
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        ReportProgress(new UpdateProgress(UpdateProgressStatus.BackingUpExistingFiles));
        await _updateExistingFilesBackuper.BackupExistingFilesAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        
        ReportProgress(new UpdateProgress(UpdateProgressStatus.MovingNewFiles));
        await _updateNewFilesMover.MoveNewFiles(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        _applicationRestarter.RefreshApplicationLauncherFullName();
        ApplicationLauncherFullName = _applicationRestarter.ApplicationLauncherFullName;

        ReportProgress(new UpdateProgress(UpdateProgressStatus.RestartingApplication));
        if (!Environment.GetCommandLineArgs().Any(a => a.Equals(RegularArguments.UPDATE)))
        {
            _logger.LogInformation("UpdateApplier: Update applied successfully, restarting the application");
            await _applicationRestarter.StartNewInstance();
            
            DeleteSnippets(_updateExistingFilesBackuper.BackedUpFileSystemInfos);
                
            await _applicationRestarter.Shutdown(1);
        }
        else
        {
            DeleteSnippets(_updateExistingFilesBackuper.BackedUpFileSystemInfos);
                
            await _applicationRestarter.Shutdown(0);
        }

        return true;
    }

    private void ReportProgress(UpdateProgress updateProgress)
    {
        ((IProgress<UpdateProgress>) _updateRepository.Progress).Report(updateProgress);
    }

    private void ComputeApplicationBaseDirectory()
    {
        var applicationBaseDirectory = _updateHelperService.GetApplicationBaseDirectory()?.FullName;

        if (applicationBaseDirectory == null)
        {
            throw new ApplicationException("Unable to guess ApplicationBaseDirectory");
        }

        _updateRepository.UpdateData.ApplicationBaseDirectory = applicationBaseDirectory;
    }

    private void DeleteSnippets(List<Tuple<string, string>> backedUpFileSystemInfos)
    {
        try
        {
            _logger.LogInformation("UpdateApplier: Deleting snippet {DownloadLocation}", _updateRepository.UpdateData.DownloadLocation);
            File.Delete(_updateRepository.UpdateData.DownloadLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot delete snippet at {DownloadLocation}", _updateRepository.UpdateData.DownloadLocation);
        }
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var backedUpFileSystemInfo in backedUpFileSystemInfos)
            {
                try
                {
                    _logger.LogInformation("UpdateApplier: Deleting snippet {FileLocation}", backedUpFileSystemInfo.Item2);

                    if (File.Exists(backedUpFileSystemInfo.Item2))
                    {
                        File.Delete(backedUpFileSystemInfo.Item2);
                    }
                    else if (Directory.Exists(backedUpFileSystemInfo.Item2))
                    {
                        Directory.Delete(backedUpFileSystemInfo.Item2, true);
                    }
                    else
                    {
                        _logger.LogInformation("UpdateApplier: Can not delete snippet {FileLocation}. No such file or directory", backedUpFileSystemInfo.Item2);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot delete snippet at {FileLocation}", backedUpFileSystemInfo.Item2);
                }
            }
        }
    }

    private void ComputeDownloadLocation()
    {
        string nameCandidate = _updateRepository.UpdateData.SoftwareVersionFile.FileName;

        string fullName = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, nameCandidate);
        if (File.Exists(fullName))
        {
            int cpt = 0;

            do
            {
                cpt += 1;
                nameCandidate = $"{cpt}_{_updateRepository.UpdateData.SoftwareVersionFile.FileName}";
                fullName = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, nameCandidate);
            } 
            while (File.Exists(fullName));
        }
        
        _updateRepository.UpdateData.DownloadLocation = fullName;
    }
        
    private void ComputeUnzipLocation()
    {
        _updateRepository.UpdateData.UnzipLocation = IOUtils.Combine(_updateRepository.UpdateData.ApplicationBaseDirectory, 
            $"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
    }
}