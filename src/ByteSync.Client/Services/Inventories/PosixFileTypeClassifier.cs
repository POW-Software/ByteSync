using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using Mono.Unix;
using Mono.Unix.Native;

namespace ByteSync.Services.Inventories;

public class PosixFileTypeClassifier : IPosixFileTypeClassifier
{
    private readonly Func<string, UnixFileInfo> _unixFileInfoFactory;
    
    public PosixFileTypeClassifier()
        : this(path => new UnixFileInfo(path))
    {
    }
    
    public PosixFileTypeClassifier(Func<string, UnixFileInfo> unixFileInfoFactory)
    {
        _unixFileInfoFactory = unixFileInfoFactory;
    }
    
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
            
            var entryKind = MapFilePermissions(type);
            if (entryKind == FileSystemEntryKind.RegularFile || entryKind == FileSystemEntryKind.Unknown)
            {
                var unixKind = TryClassifyWithUnixFileInfo(path);
                if (unixKind != FileSystemEntryKind.Unknown)
                {
                    return unixKind;
                }
            }
            
            return entryKind;
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
        
        var mode = stat.st_mode;
        type = mode & FilePermissions.S_IFMT;
        
        return true;
    }
    
    private static FileSystemEntryKind MapFilePermissions(FilePermissions type)
    {
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
        
        return FileSystemEntryKind.Unknown;
    }
    
    private FileSystemEntryKind TryClassifyWithUnixFileInfo(string path)
    {
        try
        {
            var info = _unixFileInfoFactory(path);
            
            return info.FileType switch
            {
                FileTypes.BlockDevice => FileSystemEntryKind.BlockDevice,
                FileTypes.CharacterDevice => FileSystemEntryKind.CharacterDevice,
                FileTypes.Fifo => FileSystemEntryKind.Fifo,
                FileTypes.Socket => FileSystemEntryKind.Socket,
                FileTypes.Directory => FileSystemEntryKind.Directory,
                FileTypes.RegularFile => FileSystemEntryKind.RegularFile,
                FileTypes.SymbolicLink => FileSystemEntryKind.Symlink,
                _ => FileSystemEntryKind.Unknown
            };
        }
        catch (Exception)
        {
            return FileSystemEntryKind.Unknown;
        }
    }
}
