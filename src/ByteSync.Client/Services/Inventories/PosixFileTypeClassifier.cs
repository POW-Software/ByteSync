using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using Mono.Unix.Native;

namespace ByteSync.Services.Inventories;

public class PosixFileTypeClassifier : IPosixFileTypeClassifier
{
    public FileSystemEntryKind ClassifyPosixEntry(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return FileSystemEntryKind.Unknown;
        }

        try
        {
            if (!TryGetMode(path, out var type))
            {
                return FileSystemEntryKind.Unknown;
            }

            if (type == FilePermissions.S_IFREG)
            {
                return FileSystemEntryKind.RegularFile;
            }

            if (type == FilePermissions.S_IFDIR)
            {
                return FileSystemEntryKind.Directory;
            }

            if (type == FilePermissions.S_IFBLK)
            {
                return FileSystemEntryKind.BlockDevice;
            }

            if (type == FilePermissions.S_IFCHR)
            {
                return FileSystemEntryKind.CharacterDevice;
            }

            if (type == FilePermissions.S_IFIFO)
            {
                return FileSystemEntryKind.Fifo;
            }

            if (type == FilePermissions.S_IFSOCK)
            {
                return FileSystemEntryKind.Socket;
            }

            if (type == FilePermissions.S_IFLNK)
            {
                return FileSystemEntryKind.Symlink;
            }
        }
        catch (DllNotFoundException)
        {
            return FileSystemEntryKind.Unknown;
        }
        catch (EntryPointNotFoundException)
        {
            return FileSystemEntryKind.Unknown;
        }
        catch (PlatformNotSupportedException)
        {
            return FileSystemEntryKind.Unknown;
        }

        return FileSystemEntryKind.Unknown;
    }

    private static bool TryGetMode(string path, out FilePermissions type)
    {
        if (Syscall.lstat(path, out var stat) != 0)
        {
            if (Syscall.stat(path, out stat) != 0)
            {
                type = 0;
                return false;
            }
        }

        var mode = (FilePermissions)stat.st_mode;
        type = mode & FilePermissions.S_IFMT;
        return true;
    }
}
