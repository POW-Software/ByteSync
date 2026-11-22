using ByteSync.Interfaces.Controls.Inventories;
﻿using System.IO;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Services.Synchronizations;

public class FileDatesSetter : IFileDatesSetter
{
    private readonly ILocalSourceAuthorizationService _authorization;

    private readonly ILogger<FileDatesSetter> _logger;

    public FileDatesSetter(ILogger<FileDatesSetter> logger, ILocalSourceAuthorizationService authorization)
    {
        _authorization = authorization;
        _logger = logger;
    }
    
    public Task SetDates(SharedFileDefinition sharedFileDefinition, string finalDestination, DownloadTargetDates? downloadTargetDates)
    {
        return Task.Run(() =>
        {
            _authorization.ValidatePath(finalDestination);
            if (downloadTargetDates != null)
            {
                _logger.LogInformation("{sharedFileDefinitionId}: Setting CreationTime and LastWriteTime on {FinalDestination}", 
                    sharedFileDefinition.Id, finalDestination);
                
                LocalPathSafety.SetDates(finalDestination, downloadTargetDates.CreationTimeUtc, downloadTargetDates.LastWriteTimeUtc);
            }
            else
            {
                _logger.LogInformation("{sharedFileDefinitionId}: Setting CreationTime and LastWriteTime on {FinalDestination} to now", 
                    sharedFileDefinition.Id, finalDestination);
                
                LocalPathSafety.SetDates(finalDestination, DateTime.UtcNow, DateTime.UtcNow);
            }
        });
    }

    public Task SetDates(SharedActionsGroup sharedActionsGroup, string finalDestination, DownloadTargetDates? downloadTargetDates)
    {
        return Task.Run(() =>
        {
            _authorization.ValidatePath(finalDestination);
            if (downloadTargetDates != null)
            {
                _logger.LogInformation("{Type:l}: Setting CreationTime and LastWriteTime on {FinalDestination}", 
                    $"Synchronization.{sharedActionsGroup.Operator}", finalDestination);
                
                LocalPathSafety.SetDates(finalDestination, downloadTargetDates.CreationTimeUtc, downloadTargetDates.LastWriteTimeUtc);
            }
            else
            {
                _logger.LogInformation("{Type:l}: Setting CreationTime and LastWriteTime on {FinalDestination} to now", 
                    $"Synchronization.{sharedActionsGroup.Operator}", finalDestination);
                
                LocalPathSafety.SetDates(finalDestination, DateTime.UtcNow, DateTime.UtcNow);
            }
        });
    }
}