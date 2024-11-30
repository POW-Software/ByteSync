using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class SynchronizationStatusCheckerService : ISynchronizationStatusCheckerService
{
    private readonly ILogger<SynchronizationStatusCheckerService> _logger;

    public SynchronizationStatusCheckerService(ILogger<SynchronizationStatusCheckerService> logger)
    {
        _logger = logger;
    }
    
    public bool CheckSynchronizationCanBeUpdated(SynchronizationEntity? synchronization)
    {
        return CheckSynchronization(synchronization, true);
    }
    
    public bool CheckSynchronizationCanBeAborted(SynchronizationEntity? synchronization)
    {
        return CheckSynchronization(synchronization, false);
    }

    private bool CheckSynchronization(SynchronizationEntity? synchronization, bool checkIsAborted)
    {
        if (synchronization == null)
        {
            _logger.LogWarning("CheckSynchronization: Synchronization is null");
            return false;
        }

        if (checkIsAborted)
        {
            if (synchronization.IsAbortRequested)
            {
                _logger.LogWarning("CheckSynchronization: Synchronization abortion is requested");
                return false;
            }

        }
        
        if (synchronization.IsEnded)
        {
            _logger.LogWarning("CheckSynchronization: Synchronization is ended");
            return false;
        }

        return true;
    }
}