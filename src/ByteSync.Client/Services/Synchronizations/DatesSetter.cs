using System.IO;
using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Services.Synchronizations;

public class DatesSetter : IDatesSetter
{
    private readonly ILogger<DatesSetter> _logger;

    public DatesSetter(ILogger<DatesSetter> logger)
    {
        _logger = logger;
    }
    
    public Task SetDates(SharedFileDefinition sharedFileDefinition, string finalDestination, DownloadTargetDates? downloadTargetDates)
    {
        return Task.Run(() =>
        {
            if (downloadTargetDates != null)
            {
                _logger.LogInformation("{sharedFileDefinitionId}: Setting CreationTime and LastWriteTime on {FinalDestination}", 
                    sharedFileDefinition.Id, finalDestination);
                
                File.SetCreationTimeUtc(finalDestination, downloadTargetDates.CreationTimeUtc);
                File.SetLastWriteTimeUtc(finalDestination, downloadTargetDates.LastWriteTimeUtc);
            }
            else
            {
                _logger.LogInformation("{sharedFileDefinitionId}: Setting CreationTime and LastWriteTime on {FinalDestination} to now", 
                    sharedFileDefinition.Id, finalDestination);
                
                File.SetCreationTimeUtc(finalDestination, DateTime.UtcNow);
                File.SetLastWriteTimeUtc(finalDestination, DateTime.UtcNow);
            }
        });
    }

    public Task SetDates(SharedActionsGroup sharedActionsGroup, string finalDestination, DownloadTargetDates? downloadTargetDates)
    {
        return Task.Run(() =>
        {
            if (downloadTargetDates != null)
            {
                _logger.LogInformation("{Type:l}: Setting CreationTime and LastWriteTime on {FinalDestination}", 
                    $"Synchronization.{sharedActionsGroup.Operator}", finalDestination);
                
                File.SetCreationTimeUtc(finalDestination, downloadTargetDates.CreationTimeUtc);
                File.SetLastWriteTimeUtc(finalDestination, downloadTargetDates.LastWriteTimeUtc);
            }
            else
            {
                _logger.LogInformation("{Type:l}: Setting CreationTime and LastWriteTime on {FinalDestination} to now", 
                    $"Synchronization.{sharedActionsGroup.Operator}", finalDestination);
                
                File.SetCreationTimeUtc(finalDestination, DateTime.UtcNow);
                File.SetLastWriteTimeUtc(finalDestination, DateTime.UtcNow);
            }
        });
    }
}