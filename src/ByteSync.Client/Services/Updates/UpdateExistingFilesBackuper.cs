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

    public async Task BackupExistingFilesAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() => BackupExistingFiles(cancellationToken));
    }

    private void BackupExistingFiles(CancellationToken cancellationToken)
    {
        DirectoryInfo applicationBaseDirectoryInfo = new DirectoryInfo(_updateRepository.UpdateData.ApplicationBaseDirectory);
        
        foreach (var fileSystemInfo in applicationBaseDirectoryInfo.GetFileSystemInfos())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("UpdateExistingFilesBackuper.BackupExistingFiles: Cancellation requested");
                
                return;
            }
            
            if (fileSystemInfo is DirectoryInfo)
            {
                if (!fileSystemInfo.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) &&
                    !fileSystemInfo.Name.Equals("ByteSync.app", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation("UpdateExistingFilesBackuper.BackupExistingFiles: ignored directory {directory}", fileSystemInfo.FullName);
                    
                    continue;
                }
            }

            if (fileSystemInfo is FileInfo fi)
            {
                // Si l'une des conditions est réunies
                //  - Le Nom ne contient pas ByteSync
                //  - Son extension est dans .log, .dat, .xml, .json ou .zip
                //  - Il commence par unins et finit par .exe
                // => On l'ignore
                if (!fileSystemInfo.Name.Contains("ByteSync", StringComparison.InvariantCultureIgnoreCase) ||
                    fi.Extension.ToLower().In(".log", ".dat", ".xml", ".json", ".zip") ||
                    (fi.Name.StartsWith("unins", StringComparison.InvariantCultureIgnoreCase)
                     && fi.Extension.Equals(".exe", StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.LogInformation("UpdateExistingFilesBackuper.BackupExistingFiles: ignored file {file}", fileSystemInfo.FullName);
                    
                    continue;
                }
            }
            
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
}