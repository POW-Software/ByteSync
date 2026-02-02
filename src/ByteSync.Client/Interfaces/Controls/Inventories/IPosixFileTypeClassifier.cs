using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IPosixFileTypeClassifier
{
    FileSystemEntryKind ClassifyPosixEntry(string path);
}
