using System.Net;
using System.Text;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Controls.Json;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Trusts;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class TrustFunctionTests
{

    
    [Test]
    public async Task StartTrustCheck_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        StartTrustCheckRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<StartTrustCheckRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (StartTrustCheckRequest)r)
            .ReturnsAsync(new StartTrustCheckResult { IsOK = true });
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new TrustCheckParameters { SessionId = "S1", MembersInstanceIdsToCheck = ["VI"] };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.StartTrustCheck(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task GiveMemberPublicKeyCheckData_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        GiveMemberPublicKeyCheckDataRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GiveMemberPublicKeyCheckDataRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (GiveMemberPublicKeyCheckDataRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new GiveMemberPublicKeyCheckDataParameters { SessionId = "S1", PublicKeyCheckData = new PublicKeyCheckData() };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GiveMemberPublicKeyCheckData(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task SendDigitalSignatures_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        SendDigitalSignaturesRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<SendDigitalSignaturesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (SendDigitalSignaturesRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new SendDigitalSignaturesParameters { DataId = "S1", DigitalSignatureCheckInfos = new List<DigitalSignatureCheckInfo>() };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.SendDigitalSignatures(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.DataId.Should().Be("S1");
    }

    [Test]
    public async Task SetAuthChecked_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        SetAuthCheckedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<SetAuthCheckedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (SetAuthCheckedRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new SetAuthCheckedParameters { SessionId = "S1" };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.SetAuthChecked(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task RequestTrustPublicKey_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        RequestTrustPublicKeyRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RequestTrustPublicKeyRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (RequestTrustPublicKeyRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new RequestTrustProcessParameters { SessionId = "S1" };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.RequestTrustPublicKey(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S1");
    }

    [Test]
    public async Task InformPublicKeyValidationIsFinished_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        InformPublicKeyValidationIsFinishedRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<InformPublicKeyValidationIsFinishedRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (InformPublicKeyValidationIsFinishedRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new PublicKeyValidationParameters { SessionId = "S1" };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.InformPublicKeyValidationIsFinished(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.SessionId.Should().Be("S1");
    }
    
    [Test]
    public async Task InformProtocolVersionIncompatible_ForwardsRequest_AndReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        InformProtocolVersionIncompatibleRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<InformProtocolVersionIncompatibleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (InformProtocolVersionIncompatibleRequest)r)
            .Returns(Task.CompletedTask);
        
        var function = new TrustFunction(mediatorMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = "test-session-id",
            MemberClientInstanceId = "member-instance-id",
            JoinerClientInstanceId = "joiner-instance-id",
            MemberProtocolVersion = ProtocolVersion.CURRENT,
            JoinerProtocolVersion = 0
        };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.InformProtocolVersionIncompatible(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Parameters.Should().NotBeNull();
        captured.Parameters.SessionId.Should().Be("test-session-id");
        captured.Parameters.MemberClientInstanceId.Should().Be("member-instance-id");
        captured.Parameters.JoinerClientInstanceId.Should().Be("joiner-instance-id");
        captured.Parameters.MemberProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
        captured.Parameters.JoinerProtocolVersion.Should().Be(0);
        captured.Client.Should().NotBeNull();
    }
}