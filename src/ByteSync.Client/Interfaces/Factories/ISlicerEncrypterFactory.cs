using ByteSync.Interfaces.Controls.Encryptions;

namespace ByteSync.Interfaces.Factories;

public interface ISlicerEncrypterFactory
{
    ISlicerEncrypter Create();
} 