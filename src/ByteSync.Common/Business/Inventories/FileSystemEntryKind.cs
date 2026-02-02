namespace ByteSync.Common.Business.Inventories;

public enum FileSystemEntryKind
{
    Unknown = 0,
    RegularFile = 1,
    Directory = 2,
    BlockDevice = 3,
    CharacterDevice = 4,
    Fifo = 5,
    Socket = 6,
    Symlink = 7
}
