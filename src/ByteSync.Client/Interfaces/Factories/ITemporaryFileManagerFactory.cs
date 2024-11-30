using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Interfaces.Factories;

public interface ITemporaryFileManagerFactory
{
    ITemporaryFileManager Create(string destinationFullName);
}