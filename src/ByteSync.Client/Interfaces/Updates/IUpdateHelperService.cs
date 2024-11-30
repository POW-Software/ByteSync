using System.IO;

namespace ByteSync.Interfaces.Updates;

public interface IUpdateHelperService
{
    public DirectoryInfo? GetApplicationBaseDirectory();
}