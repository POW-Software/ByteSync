using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public class FilePartUploadAsserter : IFilePartUploadAsserter
{
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ISessionService _sessionService;

    public FilePartUploadAsserter(IFileTransferApiClient fileTransferApiClient, ISessionService sessionService)
    {
        _fileTransferApiClient = fileTransferApiClient;
        _sessionService = sessionService;
    }

    public async Task AssertFilePartIsUploaded(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var transferParameters = new TransferParameters
        {
            SessionId = sharedFileDefinition.SessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber
        };
        
        await _fileTransferApiClient.AssertFilePartIsUploaded(transferParameters);
    }

    public async Task AssertUploadIsFinished(SharedFileDefinition sharedFileDefinition, int totalParts)
    {
        var sessionId = !string.IsNullOrWhiteSpace(_sessionService.SessionId) 
            ? _sessionService.SessionId 
            : sharedFileDefinition.SessionId;
            
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = totalParts
        };
        
        await _fileTransferApiClient.AssertUploadIsFinished(transferParameters);
    }
} 