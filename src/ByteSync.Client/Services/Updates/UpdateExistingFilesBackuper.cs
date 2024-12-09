using System.IO;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using Serilog;

namespace ByteSync.Services.Updates;

public class UpdateExistingFilesBackuper
{
    public const string BAK_EXTENSION = "pow_upd_bak";
    
    public UpdateExistingFilesBackuper(SoftwareVersionFile softwareVersionFile)
    {
        SoftwareVersionFile = softwareVersionFile;

        BackedUpFileSystemInfos = new List<Tuple<string, string>>();
    }
    
    private SoftwareVersionFile SoftwareVersionFile { get; }
    
    public List<Tuple<string, string>> BackedUpFileSystemInfos { get; }

    public async Task BackupExistingFilesAsync(string applicationBaseDirectory)
    {
        await Task.Run(() => BackupExistingFiles(applicationBaseDirectory));
    }

    private void BackupExistingFiles(string applicationBaseDirectory)
    {
        DirectoryInfo applicationBaseDirectoryInfo = new DirectoryInfo(applicationBaseDirectory);
        
        foreach (var fileSystemInfo in applicationBaseDirectoryInfo.GetFileSystemInfos())
        {
            if (fileSystemInfo is DirectoryInfo)
            {
                if (!fileSystemInfo.Name.Equals("Contents", StringComparison.InvariantCultureIgnoreCase) &&
                    !fileSystemInfo.Name.Equals("ByteSync.app", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Information("UpdateExistingFilesBackuper.BackupExistingFiles: ignored directory {directory}", fileSystemInfo.FullName);
                    
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
                    Log.Information("UpdateExistingFilesBackuper.BackupExistingFiles: ignored file {file}", fileSystemInfo.FullName);
                    
                    continue;
                }
            }
            
            string previousFullName = fileSystemInfo.FullName;
            
            int cpt = 0;
            var backupDestination = $"{fileSystemInfo.FullName}.{BAK_EXTENSION}{cpt}";
            
            while (File.Exists(backupDestination) || Directory.Exists(backupDestination))
            {
                cpt += 1;
                backupDestination = $"{fileSystemInfo.FullName}.{BAK_EXTENSION}{cpt}";
            }
            
            Log.Information("UpdateExistingFilesBackuper: Renaming {Source} to {Destination}", previousFullName, backupDestination);

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