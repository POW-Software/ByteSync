using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ByteSync.Interfaces.Updates;
using Serilog;

namespace ByteSync.Services.Updates;

public class DeleteUpdateBackupSnippetsService : IDeleteUpdateBackupSnippetsService
{
    private readonly IUpdateHelperService _updateHelperService;
    private readonly ILogger<DeleteUpdateBackupSnippetsService> _logger;

    public DeleteUpdateBackupSnippetsService(IUpdateHelperService updateHelperService, ILogger<DeleteUpdateBackupSnippetsService> logger)
    {
        _updateHelperService = updateHelperService;
        _logger = logger;
    }
    
    private DirectoryInfo? ApplicationBaseDirectory => _updateHelperService.GetApplicationBaseDirectory();
    
    public async Task DeleteBackupSnippetsAsync()
    {
        await Task.Run(DeleteBackupSnippets);
    }

    private void DeleteBackupSnippets()
    {
        if (ApplicationBaseDirectory == null)
        {
            _logger.LogWarning("Unable to guess ApplicationBaseDirectory");
            return;
        }
        
        Regex regex = new Regex(@$"{UpdateReplacer.BAK_EXTENSION}\d+$");
        foreach (var fileSystemInfo in ApplicationBaseDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            if (regex.IsMatch(fileSystemInfo.Name))
            {
                try
                {
                    Log.Information("UpdateBackupSnippetsDeleter: Deleting update snippet {FileInfo}", fileSystemInfo.FullName);

                    if (fileSystemInfo is DirectoryInfo directoryInfo)
                    {
                        directoryInfo.Delete(true);
                    }
                    else
                    {
                        fileSystemInfo.Delete();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while deleting {FileInfo}", fileSystemInfo.FullName);
                }
            }
        }

    }
}