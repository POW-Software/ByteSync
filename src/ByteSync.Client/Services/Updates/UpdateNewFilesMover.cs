using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

public class UpdateNewFilesMover : IUpdateNewFilesMover
{
    private readonly IUpdateRepository _updateRepository;
    private readonly ILogger<UpdateNewFilesMover> _logger;

    public UpdateNewFilesMover(IUpdateRepository updateRepository, ILogger<UpdateNewFilesMover> logger)
    {
        _updateRepository = updateRepository;
        _logger = logger;
    }
    
    public Task MoveNewFiles(CancellationToken cancellationToken)
    {
        if (_updateRepository.UpdateData.UnzipLocation.Equals(_updateRepository.UpdateData.ApplicationBaseDirectory))
        {
            _logger.LogInformation("UpdateNewFilesMover: Unzip location is the same as the application base directory, skipping moving new files");
            return Task.CompletedTask;
        }
        
        foreach (var updateFile in Directory.GetFiles(_updateRepository.UpdateData.UnzipLocation))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            
            var fileName = Path.GetFileName(updateFile);
            var destinationFile = Path.Combine(_updateRepository.UpdateData.ApplicationBaseDirectory, fileName);
            _logger.LogInformation("UpdateNewFilesMover: Moving {UpdateFile} to {DestinationFile}", updateFile, destinationFile);
            
            File.Move(updateFile, destinationFile);
        }
        
        return Task.CompletedTask;
    }
}