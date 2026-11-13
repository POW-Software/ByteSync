using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Security;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Security;

public class SessionAuthorizationSecurityTests
{
    public static IEnumerable<Type> SessionRequests => typeof(ISessionRequest).Assembly.GetTypes()
        .Where(t => !t.IsInterface && typeof(ISessionRequest).IsAssignableFrom(t));

    private static Client Client() => new() { Identity = new string('A', 64), AuthenticationEpoch = AuthenticationProtocol.Epoch,
        ClientId = new string('A', 64), ClientInstanceId = new string('A', 64) + "_" + Guid.NewGuid().ToString("D") };

    private static object Request(Type type, Client client)
    {
        var constructor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
        var args = constructor.GetParameters().Select(p =>
        {
            if (p.ParameterType == typeof(Client)) return (object)client;
            if (p.ParameterType == typeof(string)) return p.Name == "clientInstanceId" ? client.ClientInstanceId : "session";
            var value = Activator.CreateInstance(p.ParameterType);
            p.ParameterType.GetProperty("SessionId")?.SetValue(value, "session");
            return value;
        }).ToArray();
        return constructor.Invoke(args);
    }

    [TestCaseSource(nameof(SessionRequests))]
    public async Task NonMember_OnEverySessionRequest_CannotReadMutateOrNotify(Type requestType)
    {
        var client = Client();
        var repository = A.Fake<ICloudSessionsRepository>();
        var transfers = A.Fake<ITransferAuthorizationService>();
        A.CallTo(() => repository.Get("session")).Returns(new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, ProtocolVersion = ProtocolVersion.CURRENT });
        var behavior = new SessionAuthorizationBehavior<object, object>(repository, transfers, A.Fake<ITrackingActionRepository>(), A.Fake<IInventoryRepository>(), A.Fake<ILobbyRepository>());
        var calls = 0;
        var request = Request(requestType, client);
        if (requestType.Name is "AskPasswordExchangeKeyRequest" or "BeginJoinSessionRequest")
        {
            var data = requestType.GetProperty("Parameters")!.GetValue(request)!;
            data.GetType().GetProperty("PublicKeyInfo")!.SetValue(data, new ByteSync.Common.Business.EndPoints.PublicKeyInfo { PublicKey = [] });
        }

        var act = () => behavior.Handle(request, _ => { calls++; return Task.FromResult(new object()); }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        calls.Should().Be(0);
        A.CallTo(transfers).MustNotHaveHappened();
    }

    [Test]
    public async Task Member_CanReadAnotherMembersSources()
    {
        var client = Client();
        var repository = A.Fake<ICloudSessionsRepository>();
        var session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, ProtocolVersion = ProtocolVersion.CURRENT };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        A.CallTo(() => repository.Get("session")).Returns(session);
        var behavior = new SessionAuthorizationBehavior<object, object>(repository, A.Fake<ITransferAuthorizationService>(), A.Fake<ITrackingActionRepository>(), A.Fake<IInventoryRepository>(), A.Fake<ILobbyRepository>());
        var request = new GetDataSourcesRequest("session", "another-member", "another-node", client);
        if (request is IFileTransferRequest transfer) { transfer.TransferParameters.PartNumber = 1; transfer.TransferParameters.TotalParts = 1; }
        var called = false;

        await behavior.Handle(request, _ => { called = true; return Task.FromResult(new object()); }, CancellationToken.None);

        called.Should().BeTrue();
    }

    [Test]
    public async Task Member_CannotModifyAnotherMembersSources()
    {
        var client = Client();
        var repository = A.Fake<ICloudSessionsRepository>();
        var session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, ProtocolVersion = ProtocolVersion.CURRENT };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        A.CallTo(() => repository.Get("session")).Returns(session);
        var behavior = new SessionAuthorizationBehavior<object, object>(repository, A.Fake<ITransferAuthorizationService>(), A.Fake<ITrackingActionRepository>(), A.Fake<IInventoryRepository>(), A.Fake<ILobbyRepository>());
        var request = new AddDataSourceRequest("session", client, "another-member", "node", new EncryptedDataSource());

        var act = () => behavior.Handle(request, _ => throw new AssertionException("Handler must not run"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
    [Test]
    public void SessionEntryRequests_MustDeclareTheirAuthorizationBoundary()
    {
        var namespaces = new[] { "CloudSessions", "Inventories", "SessionMembers", "Synchronizations", "FileTransfers", "Trusts" };
        var requests = typeof(ISessionRequest).Assembly.GetTypes().Where(t => t.IsClass && t.Name.EndsWith("Request") &&
            namespaces.Any(n => t.Namespace == "ByteSync.ServerCommon.Commands." + n) &&
            t.Name is not ("CreateSessionRequest" or "SendDigitalSignaturesRequest"));

        requests.Should().OnlyContain(t => typeof(ISessionRequest).IsAssignableFrom(t));
    }

    [TestCaseSource(nameof(SessionRequests))]
    public async Task AuthorizedActor_CanReachTheHandlerForEverySessionOperation(Type requestType)
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var client = Client();
        client.Identity = AuthenticationProtocol.GetIdentity(rsa.ExportRSAPublicKey());
        client.ClientInstanceId = client.Identity + "_" + Guid.NewGuid().ToString("D");
        var key = new ByteSync.Common.Business.EndPoints.PublicKeyInfo { PublicKey = rsa.ExportRSAPublicKey() };
        var repository = A.Fake<ICloudSessionsRepository>();
        var inventories = A.Fake<IInventoryRepository>();
        var actions = A.Fake<ITrackingActionRepository>();
        var session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, ProtocolVersion = ProtocolVersion.CURRENT };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        session.PreSessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity,
            JoinExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1), ValidatorInstanceId = client.ClientInstanceId });
        A.CallTo(() => repository.Get("session")).Returns(session);
        A.CallTo(() => inventories.Get("session")).Returns(new ByteSync.ServerCommon.Entities.Inventories.InventoryEntity("session"));
        var behavior = new SessionAuthorizationBehavior<object, object>(repository, A.Fake<ITransferAuthorizationService>(), actions, inventories, A.Fake<ILobbyRepository>());
        var request = Request(requestType, client);
        if (request is ByteSync.ServerCommon.Commands.Synchronizations.StartSynchronizationRequest) session.IsSessionActivated = true;
        foreach (var property in requestType.GetProperties())
        {
            var value = property.GetValue(request);
            if (value == null || value is string || value is Client) continue;
            foreach (var field in value.GetType().GetProperties().Where(p => p.CanWrite))
            {
                if (field.Name is "ClientInstanceId" or "JoinerClientInstanceId" or "ValidatorInstanceId" or "JoinerInstanceId" or
                    "SessionMemberInstanceId" or "CheckedClientInstanceId" or "OtherPartyClientInstanceId" or "MemberClientInstanceId")
                    field.SetValue(value, client.ClientInstanceId);
                if (field.Name == "PublicKeyInfo") field.SetValue(value, key);
                if (field.Name == "MembersInstanceIdsToCheck") field.SetValue(value, new List<string> { client.ClientInstanceId });
                if (field.Name is "JoinerPublicKeyCheckData" or "PublicKeyCheckData")
                    field.SetValue(value, new ByteSync.Common.Business.EndPoints.PublicKeyCheckData { IssuerClientInstanceId = client.ClientInstanceId, IssuerPublicKeyInfo = key });
            }
        }
        if (request is IFileTransferRequest transfer) { transfer.TransferParameters.PartNumber = 1; transfer.TransferParameters.TotalParts = 1; }
        var called = false;

        await behavior.Handle(request, _ => { called = true; return Task.FromResult(new object()); }, CancellationToken.None);

        called.Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Member_CannotAcknowledgeActionsOwnedByAnotherInstance(bool localCopy)
    {
        var client = Client();
        var sessions = A.Fake<ICloudSessionsRepository>();
        var session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, ProtocolVersion = ProtocolVersion.CURRENT };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        A.CallTo(() => sessions.Get("session")).Returns(session);
        var actions = A.Fake<ITrackingActionRepository>();
        A.CallTo(() => actions.GetOrThrow("session", "action")).Returns(new ByteSync.ServerCommon.Entities.TrackingActionEntity { SourceClientInstanceId = "other" });
        var behavior = new SessionAuthorizationBehavior<object, object>(sessions, A.Fake<ITransferAuthorizationService>(), actions, A.Fake<IInventoryRepository>(), A.Fake<ILobbyRepository>());
        object request = localCopy ? new ByteSync.ServerCommon.Commands.Synchronizations.LocalCopyIsDoneRequest("session", client, new List<string> { "action" }, "node")
            : new ByteSync.ServerCommon.Commands.Synchronizations.SynchronizationErrorsRequest("session", client, new List<string> { "action" }, "node");

        var act = () => behavior.Handle(request, _ => throw new AssertionException("Handler must not run"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CreateSession_BindsTheCreatorToTheProvenKey(bool wrongKey)
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var client = Client();
        client.Identity = AuthenticationProtocol.GetIdentity(rsa.ExportRSAPublicKey());
        client.ClientInstanceId = client.Identity + "_" + Guid.NewGuid().ToString("D");
        var parameters = new ByteSync.Common.Business.Sessions.Cloud.Connections.CreateCloudSessionParameters
        {
            CreatorPublicKeyInfo = new ByteSync.Common.Business.EndPoints.PublicKeyInfo
            { PublicKey = rsa.ExportRSAPublicKey(), ProtocolVersion = ProtocolVersion.CURRENT }
        };
        if (wrongKey) client.Identity = new string('A', 64);
        var behavior = new SessionAuthorizationBehavior<object, object>(A.Fake<ICloudSessionsRepository>(),
            A.Fake<ITransferAuthorizationService>(), A.Fake<ITrackingActionRepository>(), A.Fake<IInventoryRepository>(), A.Fake<ILobbyRepository>());
        var called = false;
        var act = () => behavior.Handle(new ByteSync.ServerCommon.Commands.CloudSessions.CreateSessionRequest(parameters, client),
            _ => { called = true; return Task.FromResult(new object()); }, CancellationToken.None);

        if (wrongKey) await act.Should().ThrowAsync<UnauthorizedAccessException>();
        else await act();

        called.Should().Be(!wrongKey);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PasswordExchangeResponse_CannotImpersonateAnotherValidator(bool wrongKey)
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var client = Client();
        var key = rsa.ExportRSAPublicKey();
        client.Identity = AuthenticationProtocol.GetIdentity(key);
        client.ClientInstanceId = client.Identity + "_" + Guid.NewGuid().ToString("D");
        var session = new CloudSessionData { AuthenticationEpoch = AuthenticationProtocol.Epoch, ProtocolVersion = ProtocolVersion.CURRENT };
        session.SessionMembers.Add(new SessionMemberData { ClientInstanceId = client.ClientInstanceId, Identity = client.Identity });
        var sessions = A.Fake<ICloudSessionsRepository>();
        A.CallTo(() => sessions.Get("session")).Returns(session);
        var parameters = new ByteSync.Common.Business.Sessions.Cloud.Connections.GiveCloudSessionPasswordExchangeKeyParameters
        { SessionId = "session", JoinerInstanceId = "joiner", ValidatorInstanceId = wrongKey ? client.ClientInstanceId : "other-validator",
            PublicKeyInfo = new ByteSync.Common.Business.EndPoints.PublicKeyInfo { PublicKey = wrongKey ? [] : key } };
        var behavior = new SessionAuthorizationBehavior<object, object>(sessions, A.Fake<ITransferAuthorizationService>(),
            A.Fake<ITrackingActionRepository>(), A.Fake<IInventoryRepository>(), A.Fake<ILobbyRepository>());

        var act = () => behavior.Handle(new ByteSync.ServerCommon.Commands.CloudSessions.GiveCloudSessionPasswordExchangeKeyRequest(client, parameters),
            _ => throw new AssertionException("Handler must not run"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

}
