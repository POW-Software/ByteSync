using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Factories;

public class FileUploadPreparerFactory : IFileUploadPreparerFactory
{
    public IFileUploadPreparer Create()
    {
        return new FileUploadPreparer();
    }
} 