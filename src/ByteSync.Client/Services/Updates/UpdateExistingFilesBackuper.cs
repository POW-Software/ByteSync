using System.IO;
using System.Threading;
using ByteSync.Business.Updates;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

public class UpdateExistingFilesBackuper : IUpdateExistingFilesBackuper
{
    private readonly IUpdateRepository _updateRepository;
    private readonly ILogger<UpdateExistingFilesBackuper> _logger;
    
    public UpdateExistingFilesBackuper(IUpdateRepository updateRepository, ILogger<UpdateExistingFilesBackuper> logger)
    {
        _updateRepository = updateRepository;
        _logger = logger;

        BackedUpFileSystemInfos = new List<Tuple<string, string>>();
    }
    
    public List<Tuple<string, string>> BackedUpFileSystemInfos { get; }

    public Task BackupExistingFilesAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => 
        {
            try
            {
                var applicationBaseDirectoryInfo = new DirectoryInfo(_updateRepository.UpdateData.ApplicationBaseDirectory);
                var filesToBackup = GetFilesToBackup(applicationBaseDirectoryInfo, cancellationToken);
                
                foreach (var fileSystemInfo in filesToBackup)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("UpdateExistingFilesBackuper.BackupExistingFiles: Cancellation requested");
                        return;
                    }
                    
                    BackupFileSystemInfo(fileSystemInfo);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("UpdateExistingFilesBackuper.BackupExistingFiles: Operation was canceled");
                // Terminer proprement sans propager l'exception
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateExistingFilesBackuper.BackupExistingFiles: An error occurred");
                throw;
            }
        }, cancellationToken);
    }

    private IEnumerable<FileSystemInfo> GetFilesToBackup(DirectoryInfo baseDirectory, CancellationToken cancellationToken)
    {
        var result = new List<FileSystemInfo>();
        
        foreach (var fileSystemInfo in baseDirectory.GetFileSystemInfos())
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            if (fileSystemInfo is DirectoryInfo)
            {
                if (!fileSystemInfo.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) &&
                    !fileSystemInfo.Name.Equals("ByteSync.app", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation("UpdateExistingFilesBackuper.GetFilesToBackup: ignored directory {directory}", fileSystemInfo.FullName);
                    continue;
                }
            }

            if (fileSystemInfo is FileInfo fi)
            {
                // Skip files if:
                // - Name doesn't contain ByteSync
                // - Extension is .log, .dat, .xml, .json or .zip
                // - Starts with "unins" and ends with .exe
                if (!fileSystemInfo.Name.Contains("ByteSync", StringComparison.InvariantCultureIgnoreCase) ||
                    fi.Extension.ToLower().In(".log", ".dat", ".xml", ".json", ".zip") ||
                    (fi.Name.StartsWith("unins", StringComparison.InvariantCultureIgnoreCase)
                     && fi.Extension.Equals(".exe", StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.LogInformation("UpdateExistingFilesBackuper.GetFilesToBackup: ignored file {file}", fileSystemInfo.FullName);
                    continue;
                }
            }
            
            result.Add(fileSystemInfo);
        }
        
        return result;
    }
    
    private void BackupFileSystemInfo(FileSystemInfo fileSystemInfo)
    {
        string previousFullName = fileSystemInfo.FullName;
        
        int cpt = 0;
        var backupDestination = $"{fileSystemInfo.FullName}.{UpdateConstants.BAK_EXTENSION}{cpt}";
        
        while (File.Exists(backupDestination) || Directory.Exists(backupDestination))
        {
            cpt += 1;
            backupDestination = $"{fileSystemInfo.FullName}.{UpdateConstants.BAK_EXTENSION}{cpt}";
        }
        
        _logger.LogInformation("UpdateExistingFilesBackuper: Renaming {Source} to {Destination}", previousFullName, backupDestination);

        if (fileSystemInfo is FileInfo fileInfo)
        {
            fileInfo.MoveTo(backupDestination);
        }
        else if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            directoryInfo.MoveTo(backupDestination);
        }

        BackedUpFileSystemInfos.Add(new Tuple<string, string>(previousFullName, backupDestination));
    }
}
