using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IFilePartDownloadAsserter
{
    Task AssertAsync(TransferParameters parameters);
} 