using System.Threading;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Interfaces.Factories;

public interface IMergerDecrypterFactory
{
    IMergerDecrypter Build(string localPath, DownloadTarget downloadTarget, CancellationTokenSource cancellationTokenSource);
}