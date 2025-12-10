using System.Net;
using System.Text;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.CloudSessions;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class CloudSessionFunctionTests
{
    private static FunctionContext BuildFunctionContextWithClient()
    {
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>();
        mockContext.SetupGet(c => c.Items).Returns(items);

        var client = new Client("cli", "cliInst", "1.0.0", OSPlatforms.Windows, "127.0.0.1");
        items[AuthConstants.FUNCTION_CONTEXT_CLIENT] = client;

        return mockContext.Object;
    }

    private static async Task WriteBodyAsync<T>(FakeHttpRequestData request, T body)
    {
        var json = JsonHelper.Serialize(body);
        var bytes = Encoding.UTF8.GetBytes(json);
        request.Body.SetLength(0);
        await request.Body.WriteAsync(bytes, 0, bytes.Length);
        request.Body.Position = 0;
    }

    [Test]
    public async Task Create_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        CreateSessionRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (CreateSessionRequest)r)
            .ReturnsAsync(new CloudSessionResult
            {
                CloudSession = new CloudSession("S1", "cliInst"),
                SessionSettings = new EncryptedSessionSettings { Id = "S1", Data = [21], IV = [22] },
                SessionMemberInfo = new SessionMemberInfoDTO
                {
                    Endpoint = new ByteSyncEndpoint
                    {
                        ClientId = "cli",
                        ClientInstanceId = "cliInst",
                        Version = "1.0.0",
                        OSPlatform = OSPlatforms.Windows,
                        IpAddress = "127.0.0.1"
                    },
                    EncryptedPrivateData = new EncryptedSessionMemberPrivateData { Id = "priv1", Data = [23], IV = [24] },
                    SessionId = "S1",
                    JoinedSessionOn = DateTimeOffset.UtcNow,
                    PositionInList = 0
                },
                MembersIds = new List<string> { "cliInst" }
            });

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var parameters = new CreateCloudSessionParameters
        {
            LobbyId = "L1",
            CreatorProfileClientId = "creator",
            SessionSettings = new EncryptedSessionSettings { Id = "S1", Data = [1, 2], IV = [3, 4] },
            CreatorPublicKeyInfo = new PublicKeyInfo { ClientId = "cli", PublicKey = [5, 6], ProtocolVersion = 1 },
            CreatorPrivateData = new EncryptedSessionMemberPrivateData { Id = "priv", Data = [7], IV = [8] }
        };
        await WriteBodyAsync(request, parameters);

        var response = await function.Create(request, context);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.CreateCloudSessionParameters.LobbyId.Should().Be("L1");
        captured.Client.ClientId.Should().Be("cli");
        response.Body.Length.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task AskPasswordExchangeKey_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        AskPasswordExchangeKeyRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AskPasswordExchangeKeyRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AskPasswordExchangeKeyRequest)r)
            .ReturnsAsync(JoinSessionResult.BuildProcessingNormally());

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var parameters = new AskCloudSessionPasswordExchangeKeyParameters
        {
            SessionId = "S1",
            PublicKeyInfo = new PublicKeyInfo { ClientId = "cli", PublicKey = [9], ProtocolVersion = 2 },
            LobbyId = "L2",
            ProfileClientId = "prof"
        };
        await WriteBodyAsync(request, parameters);

        var response = await function.AskPasswordExchangeKey(request, context, "S1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S1");
        captured.Client.ClientInstanceId.Should().Be("cliInst");
        response.Body.Length.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task ValidateJoinCloudSession_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        ValidateJoinCloudSessionRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<ValidateJoinCloudSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (ValidateJoinCloudSessionRequest)r)
            .ReturnsAsync(Unit.Value);

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = new Mock<FunctionContext>().Object;
        var request = new FakeHttpRequestData(context);

        var parameters = new ValidateJoinCloudSessionParameters
        {
            SessionId = "S2",
            JoinerClientInstanceId = "joiner",
            ValidatorInstanceId = "validator",
            EncryptedAesKey = [10, 11],
            FinalizationPassword = "pwd"
        };
        await WriteBodyAsync(request, parameters);

        var response = await function.ValidateJoinCloudSession(request, context, "S2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S2");
    }

    [Test]
    public async Task FinalizeJoinCloudSession_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        FinalizeJoinCloudSessionRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<FinalizeJoinCloudSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (FinalizeJoinCloudSessionRequest)r)
            .ReturnsAsync(FinalizeJoinSessionResult.BuildFrom(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess));

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var parameters = new FinalizeJoinCloudSessionParameters
        {
            SessionId = "S3",
            JoinerInstanceId = "joiner2",
            ValidatorInstanceId = "validator2",
            FinalizationPassword = "finalPwd",
            EncryptedSessionMemberPrivateData = new EncryptedSessionMemberPrivateData { Id = "mid", Data = [12], IV = [13] }
        };
        await WriteBodyAsync(request, parameters);

        var response = await function.FinalizeJoinCloudSession(request, context, "S3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.FinalizeJoinCloudSessionParameters.SessionId.Should().Be("S3");
        captured.Client.ClientInstanceId.Should().Be("cliInst");
        response.Body.Length.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task AskJoinCloudSession_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        AskJoinCloudSessionRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AskJoinCloudSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AskJoinCloudSessionRequest)r)
            .ReturnsAsync(JoinSessionResult.BuildProcessingNormally());

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var parameters = new AskJoinCloudSessionParameters
        {
            SessionId = "S4",
            JoinerClientInstanceId = "joiner3",
            ValidatorInstanceId = "validator3",
            EncryptedPassword = [14, 15]
        };
        await WriteBodyAsync(request, parameters);

        var response = await function.AskJoinCloudSession(request, context, "S4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S4");
        captured.Client.ClientInstanceId.Should().Be("cliInst");
        response.Body.Length.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GiveCloudSessionPasswordExchangeKey_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        GiveCloudSessionPasswordExchangeKeyRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GiveCloudSessionPasswordExchangeKeyRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GiveCloudSessionPasswordExchangeKeyRequest)r)
            .ReturnsAsync(Unit.Value);

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var parameters = new GiveCloudSessionPasswordExchangeKeyParameters
        {
            SessionId = "S5",
            JoinerInstanceId = "joiner4",
            ValidatorInstanceId = "validator4",
            PublicKeyInfo = new PublicKeyInfo { ClientId = "cli", PublicKey = [16], ProtocolVersion = 3 }
        };
        await WriteBodyAsync(request, parameters);

        var response = await function.GiveCloudSessionPasswordExchangeKey(request, context, "S5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S5");
        captured.Client.ClientInstanceId.Should().Be("cliInst");
    }

    [Test]
    public async Task InformPasswordIsWrong_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        InformPasswordIsWrongRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<InformPasswordIsWrongRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (InformPasswordIsWrongRequest)r)
            .ReturnsAsync(Unit.Value);

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        await WriteBodyAsync(request, "bad-client-inst");

        var response = await function.InformPasswordIsWrong(request, context, "S6");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S6");
        captured.ClientInstanceId.Should().Be("bad-client-inst");
    }

    [Test]
    public async Task UpdateSettings_ReturnsOk_WhenUpdated()
    {
        var mediatorMock = new Mock<IMediator>();
        UpdateSessionSettingsRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateSessionSettingsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (UpdateSessionSettingsRequest)r)
            .ReturnsAsync(true);

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var settings = new EncryptedSessionSettings { Id = "set1", Data = [17], IV = [18] };
        await WriteBodyAsync(request, settings);

        var response = await function.UpdateSettings(request, context, "S7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S7");
    }

    [Test]
    public async Task UpdateSettings_ReturnsConflict_WhenNotUpdated()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateSessionSettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var settings = new EncryptedSessionSettings { Id = "set2", Data = [19], IV = [20] };
        await WriteBodyAsync(request, settings);

        var response = await function.UpdateSettings(request, context, "S8");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Quit_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        QuitSessionRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<QuitSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (QuitSessionRequest)r)
            .Returns(Task.FromResult(Unit.Value));

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var response = await function.Quit(request, context, "S9");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S9");
    }

    [Test]
    public async Task Reset_ReturnsOk_AndSendsRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        ResetSessionRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<ResetSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (ResetSessionRequest)r)
            .Returns(Task.FromResult(Unit.Value));

        var function = new CloudSessionFunction(mediatorMock.Object);
        var context = BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);

        var response = await function.Reset(request, context, "S10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be("S10");
    }
}
