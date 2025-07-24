using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services.Encryptions;

namespace ByteSync.Factories;

public class SlicerEncrypterFactory : ISlicerEncrypterFactory
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILoggerFactory _loggerFactory;

    public SlicerEncrypterFactory(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ILoggerFactory loggerFactory)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _loggerFactory = loggerFactory;
    }

    public ISlicerEncrypter Create()
    {
        return new SlicerEncrypter(_cloudSessionConnectionRepository, _loggerFactory.CreateLogger<SlicerEncrypter>());
    }
} 