namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ITemporaryFileManager
{
    // void Reset(string destinationFullName);

    string GetDestinationTemporaryPath();
    
    void ValidateTemporaryFile();
    
    void TryRevertOnError(Exception exception);
}