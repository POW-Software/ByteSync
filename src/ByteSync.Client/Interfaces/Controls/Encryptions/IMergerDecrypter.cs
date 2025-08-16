using System;
using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Encryptions;

public interface IMergerDecrypter : IDisposable
{
    Task MergeAndDecrypt();
}