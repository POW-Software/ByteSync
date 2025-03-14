using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class SendDigitalSignaturesCommandHandlerTests
{
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly ILobbyRepository _mockLobbyRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<SendDigitalSignaturesCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockByteSyncClient;

    private readonly SendDigitalSignaturesCommandHandler _sendDigitalSignaturesCommandHandler;

    public SendDigitalSignaturesCommandHandlerTests()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockLobbyRepository = A.Fake<ILobbyRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<SendDigitalSignaturesCommandHandler>>();
        _mockByteSyncClient = A.Fake<IHubByteSyncPush>();

        _sendDigitalSignaturesCommandHandler = new SendDigitalSignaturesCommandHandler(
            _mockCloudSessionsRepository,
            _mockLobbyRepository,
            _mockInvokeClientsService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_CloudSession_ValidMember_SendsDigitalSignatures()
    {
        // Arrange
        var dataId = "testSession";
        var clientInstanceId = "clientInstance1";
        var recipientInstanceId = "recipientInstance";
        var client = new Client { ClientId = "client1", ClientInstanceId = clientInstanceId };
        
        var digitalSignatureCheckInfo = new DigitalSignatureCheckInfo 
        { 
            Issuer = clientInstanceId, 
            Recipient = recipientInstanceId,
            PublicKeyInfo = new PublicKeyInfo(),
        };
        
        var parameters = new SendDigitalSignaturesParameters
        {
            DataId = dataId,
            DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo> { digitalSignatureCheckInfo },
            IsAuthCheckOK = true
        };

        var cloudSession = new CloudSessionData(dataId, new EncryptedSessionSettings(), client);
        cloudSession.SessionMembers.Add(new SessionMemberData { ClientInstanceId = clientInstanceId });
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId))
            .Returns(Task.FromResult<CloudSessionData?>(cloudSession));
            
        A.CallTo(() => _mockInvokeClientsService.Client(recipientInstanceId))
            .Returns(_mockByteSyncClient);
            
        A.CallTo(() => _mockByteSyncClient.RequestCheckDigitalSignature(A<DigitalSignatureCheckInfo>.Ignored))
            .Returns(Task.CompletedTask);

        var request = new SendDigitalSignaturesRequest(parameters, client);

        // Act
        await _sendDigitalSignaturesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockLobbyRepository.Get(A<string>.Ignored)).MustNotHaveHappened();
        A.CallTo(() => _mockInvokeClientsService.Client(recipientInstanceId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncClient.RequestCheckDigitalSignature(A<DigitalSignatureCheckInfo>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, 
                A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Lobby_ValidMember_SendsDigitalSignatures()
    {
        // Arrange
        var dataId = "testLobby";
        var clientInstanceId = "clientInstance1";
        var recipientInstanceId = "recipientInstance";
        var client = new Client { ClientId = "client1", ClientInstanceId = clientInstanceId };
        
        var digitalSignatureCheckInfo = new DigitalSignatureCheckInfo 
        { 
            Issuer = clientInstanceId, 
            Recipient = recipientInstanceId,
            PublicKeyInfo = new PublicKeyInfo(),
        };
        
        var parameters = new SendDigitalSignaturesParameters
        {
            DataId = dataId,
            DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo> { digitalSignatureCheckInfo },
            IsAuthCheckOK = false
        };

        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId))
            .Returns(Task.FromResult<CloudSessionData?>(null));
            
        var profileClientId = "profileClient";
        var lobby = new Lobby { LobbyId = dataId };
        var lobbyMember = new LobbyMember(profileClientId, new PublicKeyInfo(), JoinLobbyModes.Join, client);
        var lobbyMemberCell = new LobbyMemberCell("profileClient");
        lobbyMemberCell.LobbyMember = lobbyMember;
        
        lobby.LobbyMemberCells.Add(lobbyMemberCell);

        A.CallTo(() => _mockLobbyRepository.Get(dataId))
            .Returns(Task.FromResult<Lobby?>(lobby));
            
        A.CallTo(() => _mockInvokeClientsService.Client(recipientInstanceId))
            .Returns(_mockByteSyncClient);
            
        A.CallTo(() => _mockByteSyncClient.RequestCheckDigitalSignature(A<DigitalSignatureCheckInfo>.Ignored))
            .Returns(Task.CompletedTask);

        var request = new SendDigitalSignaturesRequest(parameters, client);

        // Act
        await _sendDigitalSignaturesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockLobbyRepository.Get(dataId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(recipientInstanceId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncClient.RequestCheckDigitalSignature(A<DigitalSignatureCheckInfo>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored,
                A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustNotHaveHappened();
    }
    
    [Test]
    public async Task Handle_NonMatchingIssuer_DoesNotSendSignatures()
    {
        // Arrange
        var dataId = "testSession";
        var clientInstanceId = "clientInstance1";
        var incorrectIssuerId = "differentIssuer";
        var recipientInstanceId = "recipientInstance";
        var client = new Client { ClientId = "client1", ClientInstanceId = clientInstanceId };
        
        var digitalSignatureCheckInfo = new DigitalSignatureCheckInfo 
        { 
            Issuer = incorrectIssuerId, 
            Recipient = recipientInstanceId,
            PublicKeyInfo = new PublicKeyInfo(),
        };
        
        var parameters = new SendDigitalSignaturesParameters
        {
            DataId = dataId,
            DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo> { digitalSignatureCheckInfo }
        };

        var request = new SendDigitalSignaturesRequest(parameters, client);

        // Act
        await _sendDigitalSignaturesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(A<string>.Ignored)).MustNotHaveHappened();
        A.CallTo(() => _mockLobbyRepository.Get(A<string>.Ignored)).MustNotHaveHappened();
        A.CallTo(() => _mockInvokeClientsService.Client(A<string>.Ignored)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task Handle_CloudSession_ClientNotMember_DoesNotSendSignatures()
    {
        // Arrange
        var dataId = "testSession";
        var clientInstanceId = "clientInstance1";
        var recipientInstanceId = "recipientInstance";
        var client = new Client { ClientId = "client1", ClientInstanceId = clientInstanceId };
        
        var digitalSignatureCheckInfo = new DigitalSignatureCheckInfo 
        { 
            Issuer = clientInstanceId, 
            Recipient = recipientInstanceId,
            PublicKeyInfo = new PublicKeyInfo(),
        };
        
        var parameters = new SendDigitalSignaturesParameters
        {
            DataId = dataId,
            DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo> { digitalSignatureCheckInfo }
        };

        var cloudSession = new CloudSessionData(dataId, new EncryptedSessionSettings(), client);
        cloudSession.SessionMembers.Add(new SessionMemberData { ClientInstanceId = "differentClientId" });
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId))
            .Returns(Task.FromResult<CloudSessionData?>(cloudSession));

        var request = new SendDigitalSignaturesRequest(parameters, client);

        // Act
        await _sendDigitalSignaturesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(A<string>.Ignored)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task Handle_NoSessionOrLobby_DoesNotSendSignatures()
    {
        // Arrange
        var dataId = "nonExistentId";
        var clientInstanceId = "clientInstance1";
        var recipientInstanceId = "recipientInstance";
        var client = new Client { ClientId = "client1", ClientInstanceId = clientInstanceId };
        
        var digitalSignatureCheckInfo = new DigitalSignatureCheckInfo 
        { 
            Issuer = clientInstanceId, 
            Recipient = recipientInstanceId,
            PublicKeyInfo = new PublicKeyInfo(),
        };
        
        var parameters = new SendDigitalSignaturesParameters
        {
            DataId = dataId,
            DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo> { digitalSignatureCheckInfo }
        };

        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId))
            .Returns(Task.FromResult<CloudSessionData?>(null));
            
        A.CallTo(() => _mockLobbyRepository.Get(dataId))
            .Returns(Task.FromResult<Lobby?>(null));

        var request = new SendDigitalSignaturesRequest(parameters, client);

        // Act
        await _sendDigitalSignaturesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(dataId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockLobbyRepository.Get(dataId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(A<string>.Ignored)).MustNotHaveHappened();
    }
}