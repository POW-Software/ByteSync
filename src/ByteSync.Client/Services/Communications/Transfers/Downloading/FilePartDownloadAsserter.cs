using System.Threading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Transfers.Downloading;

public class FilePartDownloadAsserter : IFilePartDownloadAsserter
{
    
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IErrorManager _errorManager;
    private readonly ILogger<FilePartDownloadAsserter> _logger;
    
    public FilePartDownloadAsserter(IFileTransferApiClient fileTransferApiClient, SemaphoreSlim semaphoreSlim, IErrorManager errorManager, ILogger<FilePartDownloadAsserter> logger)
    {
        _fileTransferApiClient = fileTransferApiClient;
        _semaphoreSlim = semaphoreSlim;
        _errorManager = errorManager;
        _logger = logger;
    }

    public async Task AssertAsync(TransferParameters parameters)
    {
        try
        {
            await Task.Run(() => _fileTransferApiClient.AssertFilePartIsDownloaded(parameters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AssertFilePartIsDownloaded - SharedFileType:{SharedFileType} - PartNumber:{PartNumber}", 
                parameters.SharedFileDefinition.SharedFileType, parameters.PartNumber);
            await _semaphoreSlim.WaitAsync();
            try
            {
                await _errorManager.SetOnErrorAsync();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
    
} 