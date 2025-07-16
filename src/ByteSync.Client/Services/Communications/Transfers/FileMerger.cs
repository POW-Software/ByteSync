using ByteSync.Interfaces.Controls.Encryptions;
using Serilog;
using System.Threading;

namespace ByteSync.Services.Communications.Transfers;

public class FileMerger : IFileMerger
{
    
    private readonly List<IMergerDecrypter> _mergerDecrypters;
    private readonly Action<int> _onPartMerged;
    private readonly Action _onError;
    private readonly Action<int> _removeMemoryStream;
    private readonly SemaphoreSlim _semaphoreSlim;

    public FileMerger(List<IMergerDecrypter> mergerDecrypters, Action<int> onPartMerged, Action onError, Action<int> removeMemoryStream, SemaphoreSlim semaphoreSlim)
    {
        _mergerDecrypters = mergerDecrypters;
        _onPartMerged = onPartMerged;
        _onError = onError;
        _removeMemoryStream = removeMemoryStream;
        _semaphoreSlim = semaphoreSlim;
    }

    public async Task MergeAsync(int partToMerge)
    {
        try
        {
            foreach (var mergerDecrypter in _mergerDecrypters)
            {
                await mergerDecrypter.MergeAndDecrypt();
            }
            _removeMemoryStream(partToMerge);
            await _semaphoreSlim.WaitAsync();
            try
            {
                _onPartMerged(partToMerge);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        catch (Exception ex)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                _onError();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            throw;
        }
    }
    
} 