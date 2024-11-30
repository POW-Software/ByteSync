using System.Threading.Tasks;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Helpers;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Services.Communications.Transfers.AfterTransfers;

public class AfterTransferSynchronizationSharedFile : IAfterTransferSharedFile
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISynchronizationApiClient _synchronizationApiClient;

    public AfterTransferSynchronizationSharedFile(ISynchronizationService synchronizationService,
        ISynchronizationApiClient synchronizationApiClient)
    {
        _synchronizationService = synchronizationService;
        _synchronizationApiClient = synchronizationApiClient;
    }
    
    public async Task OnFilePartUploaded(SharedFileDefinition sharedFileDefinition)
    {
        await _synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.WaitUntilTrue();
    }

    public async Task OnUploadFinished(SharedFileDefinition sharedFileDefinition)
    {
        await _synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.WaitUntilTrue();
    }

    public async Task OnFilePartUploadedError(SharedFileDefinition sharedFileDefinition, Exception exception)
    {
        await _synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.WaitUntilTrue();
        
        await _synchronizationApiClient.InformSynchronizationActionError(sharedFileDefinition);
        
        // _synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.
        //
        // if (_synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.Value)
        // {
        //     
        // }
        //
        //
        // throw new NotImplementedException();
        
        // await _connectionManager.HubWrapper
        //     .AssertSynchronizationActionError(tuple.sessionId, tuple.sharedFileDefinition);
    }

    public async Task OnUploadFinishedError(SharedFileDefinition sharedFileDefinition, Exception exception)
    {
        // throw new NotImplementedException();
        
        // await _connectionManager.HubWrapper
        //     .AssertSynchronizationActionError(tuple.sessionId, tuple.sharedFileDefinition);
        
        await _synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.WaitUntilTrue();
        
        await _synchronizationApiClient.InformSynchronizationActionError(sharedFileDefinition);
    }
}