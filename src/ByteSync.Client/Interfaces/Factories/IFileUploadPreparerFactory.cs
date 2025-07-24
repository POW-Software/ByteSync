using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Interfaces.Factories;

public interface IFileUploadPreparerFactory
{
    IFileUploadPreparer Create();
} 