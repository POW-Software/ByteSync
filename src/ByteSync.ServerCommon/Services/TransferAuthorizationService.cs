using ByteSync.Common.Business.Auth;
using System.Text.Json;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class TransferAuthorizationService(ICloudSessionsRepository sessions, ISharedFilesRepository files,
    ITrackingActionRepository actions, IOptions<AppSettings> settings) : ITransferAuthorizationService
{
    public async Task Authorize(string sessionId, Client client, TransferParameters parameters, bool upload, bool create)
    {
        Security.SessionAuthorizationBehavior<object, object>.RequireIdentity(client);
        var session = await sessions.Get(sessionId);
        var supplied = parameters.SharedFileDefinition;
        if (session == null || session.IsSessionRemoved || session.AuthenticationEpoch != AuthenticationProtocol.Epoch || session.FindMember(client.ClientInstanceId)?.Identity != client.Identity ||
            supplied == null || supplied.SessionId != sessionId || parameters.SessionId != sessionId ||
            string.IsNullOrWhiteSpace(supplied.Id) || supplied.Id.Length > 100 || !Enum.IsDefined(supplied.SharedFileType) ||
            parameters.PartNumber is <= 0 or > 1000000 || parameters.TotalParts is <= 0 or > 1000000 || parameters.PartSizeInBytes is < 0)
            throw new UnauthorizedAccessException("Invalid transfer");
        if ((supplied.IsInventory || supplied.IsSynchronization || supplied.IsSynchronizationStartData) && !session.IsSessionActivated)
            throw new UnauthorizedAccessException("Session is not activated for file transfers");
        if (upload && supplied.ClientInstanceId != client.ClientInstanceId) throw new UnauthorizedAccessException();
        var recipients = session.SessionMembers.Where(m => m.ClientInstanceId != supplied.ClientInstanceId)
            .Select(m => m.ClientInstanceId).ToHashSet();
        if (supplied.IsSynchronization)
        {
            if (supplied.ActionsGroupIds == null || supplied.ActionsGroupIds.Count == 0 || supplied.ActionsGroupIds.Count > 10000)
                throw new UnauthorizedAccessException();
            HashSet<string>? targets = null;
            foreach (var id in supplied.ActionsGroupIds)
            {
                var action = await actions.GetOrThrow(sessionId, id);
                if (action.SourceClientInstanceId != supplied.ClientInstanceId) throw new UnauthorizedAccessException();
                var actionTargets = action.TargetClientInstanceAndNodeIds.Select(t => t.ClientInstanceId).ToHashSet();
                if (targets == null) targets = actionTargets;
                else targets.IntersectWith(actionTargets);
            }
            recipients.IntersectWith(targets!);
        }
        if (create)
        {
            await files.AddOrUpdate(supplied, existing =>
            {
                if (existing != null) { CheckDefinition(existing.SharedFileDefinition, supplied); return existing; }
                var definition = JsonSerializer.Deserialize<SharedFileDefinition>(JsonSerializer.Serialize(supplied))!;
                definition.StorageObjectId = Guid.NewGuid().ToString("N");
                return new SharedFileData(definition, recipients, settings.Value.DefaultStorageProvider);
            });
        }
        var record = await files.Get(supplied.Id) ?? throw new UnauthorizedAccessException("Unregistered transfer");
        CheckDefinition(record.SharedFileDefinition, supplied);
        if (!upload && (!record.Recipients.Contains(client.ClientInstanceId) ||
            (parameters.PartNumber.HasValue && !record.UploadedPartsNumbers.Contains(parameters.PartNumber.Value))))
            throw new UnauthorizedAccessException("Transfer recipient or part mismatch");
        if (record.TotalParts.HasValue && (parameters.PartNumber > record.TotalParts ||
            (parameters.TotalParts.HasValue && parameters.TotalParts != record.TotalParts))) throw new UnauthorizedAccessException();
        if (parameters.TotalParts.HasValue && record.UploadedPartsNumbers.Any(p => p > parameters.TotalParts.Value))
            throw new UnauthorizedAccessException("Total parts is inconsistent with uploaded parts");
        parameters.SharedFileDefinition = record.SharedFileDefinition;
        parameters.StorageProvider = record.StorageProvider;
    }

    private static void CheckDefinition(SharedFileDefinition stored, SharedFileDefinition supplied)
    {
        if (!stored.Equals(supplied) || stored.AdditionalName != supplied.AdditionalName || stored.IsMultiFileZip != supplied.IsMultiFileZip ||
            !(stored.IV ?? []).SequenceEqual(supplied.IV ?? []) ||
            !(stored.ActionsGroupIds ?? []).SequenceEqual(supplied.ActionsGroupIds ?? []))
            throw new UnauthorizedAccessException("Transfer definition mismatch");
    }
}
