using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Services.Communications.Transfers;

public interface IFilePartDownloadAsserter
{
    Task AssertAsync(TransferParameters parameters);
} 