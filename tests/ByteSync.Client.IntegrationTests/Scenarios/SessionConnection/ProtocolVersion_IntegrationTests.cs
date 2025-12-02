using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Server;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Services.Communications;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.IntegrationTests.Scenarios.SessionConnection;

public class ProtocolVersion_IntegrationTests : IntegrationTest
{
    private FakeHubPushHandler _fakeHubPushHandler = null!;
    private MockServerFacade _serverFacade = null!;
    private Mock<ITrustProcessPublicKeysRepository> _mockTrustProcessPublicKeysRepository = null!;
    
    [SetUp]
    public void Setup()
    {
        _fakeHubPushHandler = new FakeHubPushHandler();
        _serverFacade = new MockServerFacade();
        
        _builder.RegisterInstance(_fakeHubPushHandler).As<IHubPushHandler2>();
        
        RegisterType<PublicKeysTruster, IPublicKeysTruster>();
        
        _mockTrustProcessPublicKeysRepository = new Mock<ITrustProcessPublicKeysRepository>();
        _mockTrustProcessPublicKeysRepository
            .Setup(r => r.ResetJoinerTrustProcessData(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockTrustProcessPublicKeysRepository
            .Setup(r => r.SetExpectedPublicKeyCheckDataCount(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);
        _mockTrustProcessPublicKeysRepository
            .Setup(r => r.WaitAsync(It.IsAny<string>(), It.IsAny<Func<TrustProcessPublicKeysData, EventWaitHandle>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockTrustProcessPublicKeysRepository
            .Setup(r => r.IsProtocolVersionIncompatible(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockTrustProcessPublicKeysRepository
            .Setup(r => r.GetReceivedPublicKeyCheckData(It.IsAny<string>()))
            .ReturnsAsync(new ReadOnlyCollection<PublicKeyCheckData>(new List<PublicKeyCheckData>()));
        _builder.RegisterInstance(_mockTrustProcessPublicKeysRepository.Object).As<ITrustProcessPublicKeysRepository>();
        
        var mockPublicKeysManager = new Mock<IPublicKeysManager>();
        var myPublicKeyInfo = new PublicKeyInfo
        {
            ClientId = "joiner-client-id",
            PublicKey = Encoding.UTF8.GetBytes("test-public-key"),
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        mockPublicKeysManager.Setup(m => m.GetMyPublicKeyInfo()).Returns(myPublicKeyInfo);
        _builder.RegisterInstance(mockPublicKeysManager.Object).As<IPublicKeysManager>();
        
        var mockEnvironmentService = new Mock<IEnvironmentService>();
        mockEnvironmentService.Setup(m => m.ClientInstanceId).Returns("joiner-instance-id");
        mockEnvironmentService.Setup(m => m.ClientId).Returns("joiner-client-id");
        _builder.RegisterInstance(mockEnvironmentService.Object).As<IEnvironmentService>();
        
        var mockDialogService = new Mock<IDialogService>();
        _builder.RegisterInstance(mockDialogService.Object).As<IDialogService>();
        
        var mockFlyoutFactory = new Mock<IFlyoutElementViewModelFactory>();
        _builder.RegisterInstance(mockFlyoutFactory.Object).As<IFlyoutElementViewModelFactory>();
        
        var mockDigitalSignaturesChecker = new Mock<IDigitalSignaturesChecker>();
        _builder.RegisterInstance(mockDigitalSignaturesChecker.Object).As<IDigitalSignaturesChecker>();
        
        _builder.RegisterInstance(_serverFacade.GetTrustApiClient().Object).As<ITrustApiClient>();
        _builder.RegisterInstance(_serverFacade.GetSessionMemberApiClient().Object).As<ISessionMemberApiClient>();
        
        BuildMoqContainer();
    }
    
    [Test]
    public async Task TrustAllMembersPublicKeys_WhenServerReturnsProtocolVersionIncompatible_ShouldReturnIncompatibleStatus()
    {
        var sessionId = "test-session-123";
        
        _serverFacade
            .WithSession(sessionId, "member-instance-1")
            .WithStartTrustCheckProtocolVersionIncompatible(sessionId);
        
        var truster = Container.Resolve<IPublicKeysTruster>();
        
        var result = await truster.TrustAllMembersPublicKeys(sessionId);
        
        result.Status.Should().Be(JoinSessionStatus.IncompatibleProtocolVersion);
        result.IsOK.Should().BeFalse();
        
        _serverFacade.VerifyStartTrustCheckCalledOnce(sessionId);
    }
    
    [Test]
    public async Task TrustAllMembersPublicKeys_WhenServerReturnsSuccess_ShouldReturnOK()
    {
        var sessionId = "test-session-123";
        var memberInstanceId = "member-instance-1";
        
        _serverFacade
            .WithSession(sessionId, memberInstanceId)
            .WithStartTrustCheckSuccess(sessionId, memberInstanceId);
        
        var truster = Container.Resolve<IPublicKeysTruster>();
        
        var result = await truster.TrustAllMembersPublicKeys(sessionId);
        
        result.IsOK.Should().BeTrue();
        _serverFacade.VerifyStartTrustCheckCalledOnce(sessionId);
    }
    
    [Test]
    public async Task OnPublicKeyCheckDataAskedAsync_WhenJoinerHasIncompatibleVersion_ShouldSendNotification()
    {
        var sessionId = "test-session-123";
        var joinerInstanceId = "joiner-instance-id";
        var memberInstanceId = "joiner-instance-id";
        
        var incompatiblePublicKeyInfo = new PublicKeyInfo
        {
            ClientId = "joiner-client-id",
            PublicKey = Encoding.UTF8.GetBytes("joiner-public-key"),
            ProtocolVersion = 0
        };
        
        var truster = Container.Resolve<IPublicKeysTruster>();
        
        await truster.OnPublicKeyCheckDataAskedAsync((sessionId, joinerInstanceId, incompatiblePublicKeyInfo));
        
        _serverFacade.VerifyInformProtocolVersionIncompatibleCalledOnce();
        
        var trustApiClient = _serverFacade.GetTrustApiClient();
        trustApiClient.Verify(
            c => c.InformProtocolVersionIncompatible(
                It.Is<InformProtocolVersionIncompatibleParameters>(p =>
                    p.SessionId == sessionId &&
                    p.JoinerClientInstanceId == joinerInstanceId &&
                    p.MemberClientInstanceId == memberInstanceId &&
                    p.JoinerProtocolVersion == 0 &&
                    p.MemberProtocolVersion == ProtocolVersion.CURRENT),
                It.IsAny<CancellationToken>()),
            Moq.Times.Once);
    }
}

