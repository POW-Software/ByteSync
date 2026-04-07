using System.Net;
using System.Text;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Profiles;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Services;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class CloudSessionProfileFunctionTests
{


    [Test]
    public async Task CreateCloudSessionProfile_ForwardsRequest_AndReturnsOk()
    {
        var serviceMock = new Mock<ICloudSessionProfileService>();

        serviceMock
            .Setup(m => m.CreateCloudSessionProfile(It.IsAny<string>(), It.IsAny<Client>()))
            .ReturnsAsync(new CreateCloudSessionProfileResult());
        
        var function = new CloudSessionProfileFunction(serviceMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var sessionId = "S1";
        await HttpFunctionTestHelper.WriteBodyAsync(request, sessionId);
        
        var response = await function.CreateCloudSessionProfile(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        serviceMock.Verify(m => m.CreateCloudSessionProfile("S1", It.IsAny<Client>()), Times.Once);
    }

    [Test]
    public async Task GetCloudSessionProfileData_ForwardsRequest_AndReturnsOk()
    {
        var serviceMock = new Mock<ICloudSessionProfileService>();

        GetCloudSessionProfileDataParameters? captured = null;
        serviceMock
            .Setup(m => m.GetCloudSessionProfileData(It.IsAny<GetCloudSessionProfileDataParameters>(), It.IsAny<Client>()))
            .Callback<GetCloudSessionProfileDataParameters, Client>((p, _) => captured = p)
            .ReturnsAsync(new CloudSessionProfileData());
        
        var function = new CloudSessionProfileFunction(serviceMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new GetCloudSessionProfileDataParameters { SessionId = "P1", CloudSessionProfileId = "PC1" };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GetCloudSessionProfileData(request, context, "P1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.CloudSessionProfileId.Should().Be("PC1");
    }

    [Test]
    public async Task GetProfileDetailsPassword_ForwardsRequest_AndReturnsOk()
    {
        var serviceMock = new Mock<ICloudSessionProfileService>();

        GetProfileDetailsPasswordParameters? captured = null;
        serviceMock
            .Setup(m => m.GetProfileDetailsPassword(It.IsAny<GetProfileDetailsPasswordParameters>(), It.IsAny<Client>()))
            .Callback<GetProfileDetailsPasswordParameters, Client>((p, _) => captured = p)
            .ReturnsAsync("password123");
        
        var function = new CloudSessionProfileFunction(serviceMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new GetProfileDetailsPasswordParameters { ProfileClientId = "PC1" };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.GetProfileDetailsPassword(request, context, "P1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.ProfileClientId.Should().Be("PC1");
    }

    [Test]
    public async Task DeleteCloudSessionProfile_ForwardsRequest_AndReturnsOk()
    {
        var serviceMock = new Mock<ICloudSessionProfileService>();

        DeleteCloudSessionProfileParameters? captured = null;
        serviceMock
            .Setup(m => m.DeleteCloudSessionProfile(It.IsAny<DeleteCloudSessionProfileParameters>(), It.IsAny<Client>()))
            .Callback<DeleteCloudSessionProfileParameters, Client>((p, _) => captured = p)
            .ReturnsAsync(true);
        
        var function = new CloudSessionProfileFunction(serviceMock.Object);
        var context = HttpFunctionTestHelper.BuildFunctionContextWithClient();
        var request = new FakeHttpRequestData(context);
        
        var parameters = new DeleteCloudSessionProfileParameters { ProfileClientId = "PC1" };
        await HttpFunctionTestHelper.WriteBodyAsync(request, parameters);
        
        var response = await function.DeleteCloudSessionProfile(request, context, "P1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.ProfileClientId.Should().Be("PC1");
    }
}
