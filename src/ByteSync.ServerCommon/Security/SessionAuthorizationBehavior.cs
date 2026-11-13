using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Commands.SessionMembers;
using ByteSync.ServerCommon.Commands.Synchronizations;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;

namespace ByteSync.ServerCommon.Security;

public class SessionAuthorizationBehavior<TRequest, TResponse>(ICloudSessionsRepository sessions, ByteSync.ServerCommon.Interfaces.Services.ITransferAuthorizationService transfers, ITrackingActionRepository actions, IInventoryRepository inventories, ILobbyRepository lobbies)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is CreateSessionRequest create)
        {
            RequireIdentity(create.Client);
            RequireKey(create.Client, create.CreateCloudSessionParameters.CreatorPublicKeyInfo);
            if (create.CreateCloudSessionParameters.CreatorPublicKeyInfo.ProtocolVersion != ProtocolVersion.CURRENT) Deny();
            await RequireProfileLink(create.Client, create.CreateCloudSessionParameters.LobbyId, create.CreateCloudSessionParameters.CreatorProfileClientId, true);
        }
        if (request is ISessionRequest operation)
        {
            RequireIdentity(operation.Client);
            var session = await sessions.Get(operation.SessionId);
            if (session == null || session.IsSessionRemoved || session.AuthenticationEpoch != AuthenticationProtocol.Epoch || session.ProtocolVersion != ProtocolVersion.CURRENT)
                Deny();
            var client = operation.Client!;
            var member = session!.FindMember(client.ClientInstanceId);
            var attempt = session.PreSessionMembers.SingleOrDefault(m => m.ClientInstanceId == client.ClientInstanceId &&
                m.JoinExpiresAt > DateTimeOffset.UtcNow && m.Identity == client.Identity);
            var joining = request is AskPasswordExchangeKeyRequest or BeginJoinSessionRequest;
            var trust = request is StartTrustCheckRequest or RequestTrustPublicKeyRequest or SetAuthCheckedRequest or
                GiveMemberPublicKeyCheckDataRequest or InformPublicKeyValidationIsFinishedRequest or InformProtocolVersionIncompatibleRequest;
            var pendingAllowed = trust || request is GetMembersInstanceIdsRequest or AskJoinCloudSessionRequest or FinalizeJoinCloudSessionRequest;
            if (!joining && member == null && !(pendingAllowed && attempt != null)) Deny();
            if (member != null && member.Identity != client.Identity) Deny();
            if ((joining || pendingAllowed || request is UpdateSessionSettingsRequest or AddDataNodeRequest or RemoveDataNodeRequest or
                AddDataSourceRequest or RemoveDataSourceRequest) && session.IsSessionActivated) Deny();
            if (request is StartSynchronizationRequest && !session.IsSessionActivated) Deny();
            if (request is IActionCompletedRequest completed)
                await RequireActions(operation.SessionId, client, completed.ActionsGroupIds, completed.NodeId, request is LocalCopyIsDoneRequest);
            if (request is SynchronizationErrorsRequest errors)
                await RequireActions(operation.SessionId, client, errors.ActionsGroupIds, errors.NodeId, false, true);
            if (request is StartSynchronizationRequest start)
            {
                var inventory = await inventories.Get(operation.SessionId) ?? throw new UnauthorizedAccessException();
                foreach (var definition in start.ActionsGroupDefinitions)
                {
                    if (definition.SourceClientInstanceId != null && session.FindMember(definition.SourceClientInstanceId) == null) Deny();
                    foreach (var target in definition.TargetClientInstanceAndNodeIds)
                        if (session.FindMember(target.ClientInstanceId) == null || !inventory.InventoryMembers
                            .Any(m => m.ClientInstanceId == target.ClientInstanceId && m.DataNodes.Any(n => n.Id == target.NodeId))) Deny();
                }
            }
            switch (request)
            {
                case BeginJoinSessionRequest begin:
                    RequireKey(client, begin.Parameters.PublicKeyInfo);
                    if (begin.Parameters.LobbyId != null && begin.Parameters.LobbyId != session.LobbyId) Deny();
                    await RequireProfileLink(client, begin.Parameters.LobbyId, begin.Parameters.ProfileClientId);
                    break;
                case AskPasswordExchangeKeyRequest r:
                    RequireKey(client, r.Parameters.PublicKeyInfo);
                    if (r.Parameters.LobbyId != null && r.Parameters.LobbyId != session.LobbyId) Deny();
                    await RequireProfileLink(client, r.Parameters.LobbyId, r.Parameters.ProfileClientId);
                    break;
                case AskJoinCloudSessionRequest r:
                    if (r.Parameters.JoinerClientInstanceId != client.ClientInstanceId || attempt?.ValidatorInstanceId != r.Parameters.ValidatorInstanceId) Deny();
                    break;
                case GiveCloudSessionPasswordExchangeKeyRequest r:
                    RequireOwner(client, r.Parameters.ValidatorInstanceId);
                    RequireKey(client, r.Parameters.PublicKeyInfo);
                    break;
                case ValidateJoinCloudSessionRequest r:
                    if (r.Parameters.ValidatorInstanceId != client.ClientInstanceId || member == null) Deny();
                    break;
                case FinalizeJoinCloudSessionRequest r:
                    if (r.FinalizeJoinCloudSessionParameters.JoinerInstanceId != client.ClientInstanceId) Deny();
                    break;
                case AddDataNodeRequest r: RequireOwner(client, r.ClientInstanceId); break;
                case RemoveDataNodeRequest r: RequireOwner(client, r.ClientInstanceId); break;
                case AddDataSourceRequest r: RequireOwner(client, r.ClientInstanceId); break;
                case RemoveDataSourceRequest r: RequireOwner(client, r.ClientInstanceId); break;
                case SetGeneralStatusRequest r: RequireOwner(client, r.Parameters.ClientInstanceId); break;
                case StartTrustCheckRequest r:
                    RequireKey(client, r.Parameters.PublicKeyInfo);
                    if (r.Parameters.MembersInstanceIdsToCheck.Any(id => session.FindMember(id) == null)) Deny();
                    break;
                case RequestTrustPublicKeyRequest r:
                    RequireOwner(client, r.Parameters.JoinerClientInstanceId);
                    RequireKey(client, new PublicKeyInfo { PublicKey = r.Parameters.JoinerPublicKeyCheckData.IssuerPublicKeyInfo.PublicKey });
                    if (session.FindMember(r.Parameters.SessionMemberInstanceId) == null) Deny();
                    break;
                case GiveMemberPublicKeyCheckDataRequest r:
                    RequireOwner(client, r.Parameters.PublicKeyCheckData.IssuerClientInstanceId);
                    RequireKey(client, new PublicKeyInfo { PublicKey = r.Parameters.PublicKeyCheckData.IssuerPublicKeyInfo.PublicKey });
                    if (session.FindMemberOrPreMember(r.Parameters.ClientInstanceId) == null) Deny();
                    break;
                case SetAuthCheckedRequest r:
                    if (session.FindMemberOrPreMember(r.Parameters.CheckedClientInstanceId) == null) Deny();
                    break;
                case InformPublicKeyValidationIsFinishedRequest r:
                    if (session.FindMemberOrPreMember(r.Parameters.OtherPartyClientInstanceId) == null) Deny();
                    break;
                case InformProtocolVersionIncompatibleRequest r:
                    RequireOwner(client, r.Parameters.MemberClientInstanceId);
                    if (session.FindMemberOrPreMember(r.Parameters.JoinerClientInstanceId) == null) Deny();
                    break;
            }
        }
        if (request is IFileTransferRequest file)
        {
            if (request is not (ByteSync.ServerCommon.Commands.FileTransfers.AssertDownloadIsFinishedRequest or ByteSync.ServerCommon.Commands.FileTransfers.AssertUploadIsFinishedRequest) && file.TransferParameters.PartNumber == null) Deny();
            if (request is ByteSync.ServerCommon.Commands.FileTransfers.AssertUploadIsFinishedRequest && file.TransferParameters.TotalParts == null) Deny();
            var upload = request is ByteSync.ServerCommon.Commands.FileTransfers.GetUploadFileUrlRequest or ByteSync.ServerCommon.Commands.FileTransfers.GetUploadFileStorageLocationRequest or ByteSync.ServerCommon.Commands.FileTransfers.AssertFilePartIsUploadedRequest or ByteSync.ServerCommon.Commands.FileTransfers.AssertUploadIsFinishedRequest;
            var createTransfer = request is ByteSync.ServerCommon.Commands.FileTransfers.GetUploadFileUrlRequest or ByteSync.ServerCommon.Commands.FileTransfers.GetUploadFileStorageLocationRequest;
            await transfers.Authorize(file.SessionId, file.Client!, file.TransferParameters, upload, createTransfer);
        }
        return await next();
    }

    private async Task RequireProfileLink(Client client, string? lobbyId, string? profileId, bool creator = false)
    {
        if (lobbyId == null && profileId == null) return;
        if (lobbyId == null || profileId == null) Deny();
        var lobby = await lobbies.Get(lobbyId!);
        var member = lobby?.GetLobbyMemberByClientInstanceId(client.ClientInstanceId);
        if (lobby?.AuthenticationEpoch != AuthenticationProtocol.Epoch || member == null || member.ProfileClientId != profileId ||
            (creator && lobby.LobbyMemberCells.FirstOrDefault()?.LobbyMember?.ClientInstanceId != client.ClientInstanceId)) Deny();
    }

    private async Task RequireActions(string sessionId, Client client, List<string> ids, string? nodeId, bool requireSource, bool allowSource = false)
    {
        foreach (var id in ids)
        {
            var action = await actions.GetOrThrow(sessionId, id);
            var isSource = action.SourceClientInstanceId == client.ClientInstanceId;
            if (requireSource && !isSource) Deny();
            if (!(allowSource && isSource) && !action.TargetClientInstanceAndNodeIds
                .Any(t => t.ClientInstanceId == client.ClientInstanceId && (nodeId == null || t.NodeId == nodeId))) Deny();
        }
    }

    public static void RequireIdentity(Client? client)
    {
        if (client == null || client.AuthenticationEpoch != AuthenticationProtocol.Epoch || string.IsNullOrEmpty(client.Identity) ||
            !AuthenticationProtocol.IsInstanceOf(client.ClientInstanceId, client.Identity)) Deny();
    }

    private static void RequireOwner(Client client, string instanceId)
    {
        if (client.ClientInstanceId != instanceId) Deny();
    }

    private static void RequireKey(Client client, PublicKeyInfo key)
    {
        try
        {
            if (key == null || key.PublicKey == null || key.PublicKey.Length > 2048 || AuthenticationProtocol.GetIdentity(key.PublicKey) != client.Identity) Deny();
        }
        catch (System.Security.Cryptography.CryptographicException) { Deny(); }
    }

    private static void Deny() => throw new UnauthorizedAccessException("Operation not authorized");
}
