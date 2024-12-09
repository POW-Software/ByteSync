using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Arguments;
using ByteSync.Business.Updates;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Updates;
using ByteSync.Services.Misc;
using Serilog;
using Splat;

namespace ByteSync.Services.Updates;

public class ApplyUpdateService : IApplyUpdateService
{
    private readonly IAvailableUpdatesLister _availableUpdatesLister;
    private readonly IEnvironmentService _environmentService;
    private readonly ILocalApplicationDataManager _localApplicationDataManager;
    private readonly IUpdateHelperService _updateHelperService;

    public ApplyUpdateService(IAvailableUpdatesLister availableUpdatesLister, IEnvironmentService environmentService,
        ILocalApplicationDataManager localApplicationDataManager, IUpdateProgressRepository updateProgressRepository,
        IUpdateHelperService updateHelperService)
    {
        _availableUpdatesLister = availableUpdatesLister;
        _environmentService = environmentService;
        _localApplicationDataManager = localApplicationDataManager;
        _updateHelperService = updateHelperService;
        
        Progress = updateProgressRepository.Progress;
    }

    public string ApplicationLauncherFullName { get; private set; }
        
    public string? ApplicationBaseDirectory { get; private set; }
        
    public IProgress<UpdateProgress> Progress { get; }
        
    public string FileToDownload { get; private set; }
        
    public string DownloadLocation { get; private set; }
        
    public string UnzipLocation { get; private set; }
        
    public SoftwareVersionFile SoftwareVersionFile { get; private set; }

    public async Task<bool> Update(SoftwareVersion softwareVersion, SoftwareVersionFile softwareVersionFile, CancellationToken cancellationToken)
    {
        SoftwareVersionFile = softwareVersionFile;
            
        Log.Information("UpdateApplier: Starting update");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (_environmentService.AssemblyFullName.StartsWith("/private/var/folders/") &&
                _environmentService.AssemblyFullName.Contains("/AppTranslocation/"))
            {
                throw new Exception("Can not auto-update translocated application on MacOS");
            }
        }

        FileToDownload = _availableUpdatesLister.GetUrl(softwareVersionFile);
            
        var applicationRestarter = Locator.Current.GetService<IApplicationRestarter>()!;
        ApplicationLauncherFullName = applicationRestarter.ApplicationLauncherFullName;
        // ApplicationBaseDirectory = new FileInfo(ApplicationLauncherFullName).Directory!.FullName;

        ComputeApplicationBaseDirectory();
        ComputeDownloadLocation();
        ComputeUnzipLocation();

        var currentVersion = VersionHelper.GetVersionString(_environmentService.ApplicationVersion);

        Log.Information("UpdateApplier: Current Version: {CurrentVersion}, Update version:{Version}", currentVersion, softwareVersion.Version);
        Log.Information("UpdateApplier: ApplicationLauncherFullName:{ApplicationLauncherFullName}", ApplicationLauncherFullName);
        Log.Information("UpdateApplier: ApplicationBaseDirectory:{ApplicationBaseDirectory}", ApplicationBaseDirectory);
        Log.Information("UpdateApplier: FileToDownload:{FileToDownload}", FileToDownload);
        Log.Information("UpdateApplier: Platform:{Platform}", softwareVersionFile.Platform);
        Log.Information("UpdateApplier: Level:{Level}", softwareVersion.Level);
        Log.Information("UpdateApplier: DownloadLocation:{DownloadLocation}", DownloadLocation);
        Log.Information("UpdateApplier: UnzipLocation:{UnzipLocation}", UnzipLocation);

            
        // Téléchargement et contrôle du fichier téléchargé
        var updateDownloader = new UpdateDownloader(SoftwareVersionFile, Progress);
        await updateDownloader.Download(FileToDownload, DownloadLocation, cancellationToken); 
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        await updateDownloader.CheckDownload();

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
            
        // On force UpdateExtractor à loader sharpziplib
        var updateUnzipper = new UpdateExtractor();
        updateUnzipper.Bump();
            
        // Renommage des fichiers existants
        Progress.Report(new UpdateProgress(UpdateProgressStatus.BackingUpExistingFiles));
        var updateRenamer = new UpdateExistingFilesBackuper(softwareVersionFile);
        await updateRenamer.BackupExistingFilesAsync(ApplicationBaseDirectory!);
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
            
        // Décompression
        Progress.Report(new UpdateProgress(UpdateProgressStatus.Extracting));
        updateUnzipper = new UpdateExtractor();
        await updateUnzipper.ExtractAsync(SoftwareVersionFile, DownloadLocation, UnzipLocation);
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
            
        // Remplacement des fichiers
        // Progress.Report(new UpdateProgress(UpdateProgressStatus.UpdatingFiles));
        // var updateReplacer = new UpdateReplacer(softwareVersionFile);
        // await updateReplacer.ReplaceFilesAsync(UnzipLocation, ApplicationBaseDirectory);
        // // await updateReplacer.DeleteBackupData();

        applicationRestarter.RefreshApplicationLauncherFullName();
        ApplicationLauncherFullName = applicationRestarter.ApplicationLauncherFullName;
            
        // Suppression du fichier zip téléchargé et du répertoire de décompression
        //DeleteSnippets(updateRenamer.BackedUpFileSystemInfos);

        // CheckPermissions();

        // Lancement et fermeture du programme actuel
        Progress.Report(new UpdateProgress(UpdateProgressStatus.RestartingApplication));
        if (!Environment.GetCommandLineArgs().Any(a => a.Equals(RegularArguments.UPDATE)))
        {
            Log.Information("UpdateApplier: Update applied successfully, restarting the application");
            await applicationRestarter.StartNewInstance();
                
            // Suppression du fichier zip téléchargé et du répertoire de décompression
            DeleteSnippets(updateRenamer.BackedUpFileSystemInfos);
                
            await applicationRestarter.Shutdown(1);
        }
        else
        {
            // Suppression du fichier zip téléchargé et du répertoire de décompression
            DeleteSnippets(updateRenamer.BackedUpFileSystemInfos);
                
            await applicationRestarter.Shutdown(0);
        }

        return true;
    }

    private void ComputeApplicationBaseDirectory()
    {
        ApplicationBaseDirectory = _updateHelperService.GetApplicationBaseDirectory()?.FullName;

        if (ApplicationBaseDirectory == null)
        {
            throw new ApplicationException("Unable to guess ApplicationBaseDirectory");
        }

        // var applicationLauncher = new FileInfo(ApplicationLauncherFullName);
        //
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        // {
        //     DirectoryInfo? macOsDir = null;
        //     if (applicationLauncher.Directory != null && 
        //         applicationLauncher.Directory.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
        //     {
        //         macOsDir = applicationLauncher.Directory;
        //     }
        //     else if (applicationLauncher.Directory?.Parent != null
        //              && applicationLauncher.Directory.Parent.Name.Equals("MacOS", StringComparison.InvariantCultureIgnoreCase))
        //     {
        //         macOsDir = applicationLauncher.Directory.Parent;
        //     }
        //
        //     if (macOsDir?.Parent != null &&
        //         macOsDir.Parent.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) &&
        //         macOsDir.Parent.Parent != null &&
        //         macOsDir.Parent.Parent.Name.StartsWith("ByteSync", StringComparison.InvariantCultureIgnoreCase) &&
        //         macOsDir.Parent.Parent.Name.EndsWith(".app", StringComparison.InvariantCultureIgnoreCase))
        //     {
        //         ApplicationBaseDirectory = macOsDir.Parent.Parent.FullName;
        //     }
        // }
        // else
        // {
        //     ApplicationBaseDirectory = applicationLauncher.Directory!.FullName;
        // }
    }

    // private void CheckPermissions()
    // {
    //     // if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //     if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //     {
    //         // if (!File.Exists(ApplicationLauncherFullName))
    //         // {
    //         //     Log.Warning("CheckPermissions: File {Path} not found ", ApplicationLauncherFullName);
    //         //     
    //         //     Thread.Sleep(1000);
    //         // }
    //         
    //         if (_localApplicationDataManager.IsPortableApplication)
    //         {
    //             Log.Information("CheckPermissions: Executing chmod u+x on {Path}", ApplicationLauncherFullName);
    //
    //             UnixCommandRunner2.RunCommand("chmod", $"u+x \"{ApplicationLauncherFullName}\"");
    //             
    //             // LinuxCommandRunner linuxCommandRunner = new LinuxCommandRunner();
    //             // linuxCommandRunner.Exec($"chmod u+x \"{ApplicationLauncherFullName}\"");
    //         }
    //         else
    //         {
    //             Log.Information("CheckPermissions: Executing chmod +x on {Path}", ApplicationLauncherFullName);
    //             
    //             UnixCommandRunner2.RunCommand("chmod", $"+x \"{ApplicationLauncherFullName}\"");
    //             
    //             // LinuxCommandRunner linuxCommandRunner = new LinuxCommandRunner();
    //             // linuxCommandRunner.Exec($"chmod +x \"{ApplicationLauncherFullName}\"");
    //         }
    //
    //         // https://stackoverflow.com/questions/46481852/net-core-published-application-need-chmod-777-permissions-to-run-on-centos7
    //
    //         // https://dev.to/equiman/run-a-command-in-external-terminal-with-net-core-d4l
    //
    //
    //         // https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core
    //
    //         // https://www.howtoforge.com/tutorial/linux-chmod-command/
    //
    //
    //
    //         // https://stackoverflow.com/questions/41693683/ive-installed-dot-net-core-on-mac-but-didnt-find-dotnet-command
    //     }
    // }

    private void DeleteSnippets(List<Tuple<string, string>> backedUpFileSystemInfos)
    {
        // Suppression du fichier zip téléchargé
        try
        {
            Log.Information("UpdateApplier: Deleting snippet {DownloadLocation}", DownloadLocation);
            File.Delete(DownloadLocation);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cannot delete snippet at {DownloadLocation}", DownloadLocation);
        }
            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var backedUpFileSystemInfo in backedUpFileSystemInfos)
            {
                try
                {
                    Log.Information("UpdateApplier: Deleting snippet {FileLocation}", backedUpFileSystemInfo.Item2);

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
                        Log.Information("UpdateApplier: Can not delete snippet {FileLocation}. No such file or directory", backedUpFileSystemInfo.Item2);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cannot delete snippet at {FileLocation}", backedUpFileSystemInfo.Item2);
                }
            }
        }
    }

    private void ComputeDownloadLocation()
    {
        string nameCandidate = SoftwareVersionFile.FileName;

        string fullName = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, nameCandidate);
        if (File.Exists(fullName))
        {
            int cpt = 0;

            do
            {
                cpt += 1;
                nameCandidate = $"{cpt}_{SoftwareVersionFile.FileName}";
                fullName = IOUtils.Combine(_localApplicationDataManager.ApplicationDataPath, nameCandidate);
            } 
            while (File.Exists(fullName));
        }

        DownloadLocation = fullName;
    }
        
    private void ComputeUnzipLocation()
    {
        // string fullName;
        // int cpt = 0;
        // do
        // {
        //     cpt += 1;
        //     string nameCandidate = $"update_unzip_{cpt}";
        //     fullName = IOUtils.Combine(ApplicationBaseDirectory, nameCandidate);
        // }
        // while (File.Exists(fullName));
            
        UnzipLocation = ApplicationBaseDirectory;
    }
}