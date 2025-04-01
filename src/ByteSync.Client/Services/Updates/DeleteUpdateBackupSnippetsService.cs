using System.IO;
using System.Text.RegularExpressions;
using ByteSync.Business.Updates;
using ByteSync.Interfaces.Updates;

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
    
    public Task DeleteBackupSnippetsAsync()
    {
        if (ApplicationBaseDirectory == null)
        {
            _logger.LogWarning("Unable to guess ApplicationBaseDirectory");
            return Task.CompletedTask;
        }
        
        var regexes = new List<Regex>
        {
            new(@$"{UpdateConstants.BAK_EXTENSION}\d+$"),
            new(@$"^{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}")
        };
        
        foreach (var regex in regexes)
        {
            DeleteSnippets(regex);
        }
        
        return Task.CompletedTask;
    }

    private void DeleteSnippets(Regex regex)
    {
        foreach (var fileSystemInfo in ApplicationBaseDirectory!.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            if (regex.IsMatch(fileSystemInfo.Name))
            {
                DeleteSnippet(fileSystemInfo);
            }
        }
    }

    private void DeleteSnippet(FileSystemInfo fileSystemInfo)
    {
        try
        {
            _logger.LogInformation("UpdateBackupSnippetsDeleter: Deleting update snippet {FileInfo}", fileSystemInfo.FullName);

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