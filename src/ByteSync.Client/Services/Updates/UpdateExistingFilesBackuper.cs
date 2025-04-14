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
                
            if (fileSystemInfo is DirectoryInfo directoryInfo)
            {
                // Only include files specifically named “Contents” or “ByteSync.app”
                if (directoryInfo.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) ||
                    directoryInfo.Name.Equals("ByteSync.app", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Add(fileSystemInfo);
                }
                else
                {
                    _logger.LogInformation("UpdateExistingFilesBackuper.GetFilesToBackup: ignored directory {directory}", fileSystemInfo.FullName);
                }
            }
            else if (fileSystemInfo is FileInfo fileInfo)
            {
                // Only include files that:
                // - Contain “ByteSync” in their name
                // - Do not have a .log, .dat, .xml, .json or .zip extension
                // - Do not start with “unins” if the extension is .exe
                bool containsByteSyncName = fileInfo.Name.Contains("ByteSync", StringComparison.InvariantCultureIgnoreCase);
                bool hasAllowedExtension = !fileInfo.Extension.ToLower().In(".log", ".dat", ".xml", ".json", ".zip");
                bool isUninstaller = fileInfo.Name.StartsWith("unins", StringComparison.InvariantCultureIgnoreCase) 
                                     && fileInfo.Extension.Equals(".exe", StringComparison.InvariantCultureIgnoreCase);
                
                if (containsByteSyncName && hasAllowedExtension && !isUninstaller)
                {
                    result.Add(fileSystemInfo);
                }
                else
                {
                    _logger.LogInformation("UpdateExistingFilesBackuper.GetFilesToBackup: ignored file {file}", fileSystemInfo.FullName);
                }
            }
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

