using ByteSync.Interfaces.Controls.Encryptions;
using Serilog;

namespace ByteSync.Services.Communications.Transfers;

public class FileMerger : IFileMerger
{
    
    private readonly List<IMergerDecrypter> _mergerDecrypters;
    private readonly Action<int> _onPartMerged;
    private readonly Action _onError;
    private readonly Action<int> _removeMemoryStream;
    private readonly object _syncRoot;

    public FileMerger(List<IMergerDecrypter> mergerDecrypters, Action<int> onPartMerged, Action onError, Action<int> removeMemoryStream, object syncRoot)
    {
        _mergerDecrypters = mergerDecrypters;
        _onPartMerged = onPartMerged;
        _onError = onError;
        _removeMemoryStream = removeMemoryStream;
        _syncRoot = syncRoot;
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
            lock (_syncRoot)
            {
                _onPartMerged(partToMerge);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MergeFile");
            lock (_syncRoot)
            {
                _onError();
            }
            throw;
        }
    }
    
} 