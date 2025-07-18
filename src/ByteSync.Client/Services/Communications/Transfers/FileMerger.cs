using ByteSync.Interfaces.Controls.Encryptions;
using System.Threading;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers;

public class FileMerger : IFileMerger
{
    
    private readonly List<IMergerDecrypter> _mergerDecrypters;
    private readonly IErrorManager _errorManager;
    private readonly DownloadTarget _downloadTarget;
    private readonly SemaphoreSlim _semaphoreSlim;

    public FileMerger(List<IMergerDecrypter> mergerDecrypters, IErrorManager errorManager,
        DownloadTarget downloadTarget, SemaphoreSlim semaphoreSlim)
    {
        _mergerDecrypters = mergerDecrypters;
        _errorManager = errorManager;
        _downloadTarget = downloadTarget;
        _semaphoreSlim = semaphoreSlim;
    }
    
    public async Task MergeAsync(int partToMerge)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            try
            {
                foreach (var mergerDecrypter in _mergerDecrypters)
                {
                    await mergerDecrypter.MergeAndDecrypt();
                }

                _downloadTarget.RemoveMemoryStream(partToMerge);
            }
            catch
            {
                await _errorManager.SetOnErrorAsync();
                throw;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

    }
    
} 