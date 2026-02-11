namespace ByteSync.Models.Inventories;

public enum SkipReason
{
    Unknown = 0,
    Hidden = 1,
    SystemAttribute = 2,
    NoiseEntry = 3,
    Symlink = 4,
    SpecialPosixFile = 5,
    Offline = 6,
    Inaccessible = 7,
    NotFound = 8,
    IoError = 9
}
