using System.Net;
using System.Text;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Http;
using ByteSync.Functions.UnitTests.TestHelpers;
using ByteSync.ServerCommon.Commands.Authentication;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace ByteSync.Functions.UnitTests.Http;

[TestFixture]
public class AuthFunctionTests
{
    [Test]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        var loginData = new LoginData
        {
            ClientId = "test-client",
            ClientInstanceId = "test-instance",
            Version = "1.0.0",
            OsPlatform = OSPlatforms.Windows
        };
        
        var authResponse = new InitialAuthenticationResponse(InitialConnectionStatus.Success)
        {
            AuthenticationTokens = new AuthenticationTokens
            {
                JwtToken = "test-jwt-token",
                RefreshToken = "test-refresh-token",
                JwtTokenDurationInSeconds = 3600,
                RefreshTokenExpiration = DateTimeOffset.UtcNow.AddDays(30)
            }
        };
        
        AuthenticateRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AuthenticateRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (AuthenticateRequest)r)
            .ReturnsAsync(authResponse);
        
        var function = new AuthFunction(mediatorMock.Object);
        var mockContext = new Mock<FunctionContext>();
        var context = mockContext.Object;
        var request = new FakeHttpRequestData(context);
        request.Headers.Add("x-forwarded-for", "192.168.1.1");
        
        var json = JsonHelper.Serialize(loginData);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        request.Body.Position = 0;
        
        var response = await function.Login(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.LoginData.Should().NotBeNull();
        captured.LoginData.ClientId.Should().Be("test-client");
        captured.LoginData.ClientInstanceId.Should().Be("test-instance");
        captured.IpAddress.Should().Be("192.168.1.1");
    }
    
    [Test]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>();
        var loginData = new LoginData
        {
            ClientId = "invalid-client",
            ClientInstanceId = "test-instance",
            Version = "1.0.0",
            OsPlatform = OSPlatforms.Windows
        };
        
        var authResponse = new InitialAuthenticationResponse(InitialConnectionStatus.UndefinedClientId);
        
        mediatorMock
            .Setup(m => m.Send(It.IsAny<AuthenticateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);
        
        var function = new AuthFunction(mediatorMock.Object);
        var mockContext = new Mock<FunctionContext>();
        var context = mockContext.Object;
        var request = new FakeHttpRequestData(context);
        request.Headers.Add("x-forwarded-for", "192.168.1.1");
        
        var json = JsonHelper.Serialize(loginData);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        request.Body.Position = 0;
        
        var response = await function.Login(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        mediatorMock.Verify(m => m.Send(It.IsAny<AuthenticateRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task RefreshTokens_WithValidToken_ReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        var refreshTokensData = new RefreshTokensData
        {
            Token = "valid-refresh-token",
            ClientInstanceId = "test-instance",
            Version = "1.0.0",
            OsPlatform = OSPlatforms.Windows
        };
        
        var refreshResponse = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenOk, new AuthenticationTokens
        {
            JwtToken = "new-jwt-token",
            RefreshToken = "new-refresh-token",
            JwtTokenDurationInSeconds = 3600,
            RefreshTokenExpiration = DateTimeOffset.UtcNow.AddDays(30)
        });
        
        RefreshTokensRequest? captured = null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RefreshTokensRequest>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((r, _) => captured = (RefreshTokensRequest)r)
            .ReturnsAsync(refreshResponse);
        
        var function = new AuthFunction(mediatorMock.Object);
        var mockContext = new Mock<FunctionContext>();
        var context = mockContext.Object;
        var request = new FakeHttpRequestData(context);
        request.Headers.Add("x-forwarded-for", "10.0.0.1");
        
        var json = JsonHelper.Serialize(refreshTokensData);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        request.Body.Position = 0;
        
        var response = await function.RefreshTokens(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.RefreshTokensData.Should().NotBeNull();
        captured.RefreshTokensData.Token.Should().Be("valid-refresh-token");
        captured.RefreshTokensData.ClientInstanceId.Should().Be("test-instance");
        captured.IpAddress.Should().Be("10.0.0.1");
    }
    
    [Test]
    public async Task RefreshTokens_WithInvalidToken_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>();
        var refreshTokensData = new RefreshTokensData
        {
            Token = "invalid-refresh-token",
            ClientInstanceId = "test-instance",
            Version = "1.0.0",
            OsPlatform = OSPlatforms.Windows
        };
        
        var refreshResponse = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenNotFound);
        
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RefreshTokensRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshResponse);
        
        var function = new AuthFunction(mediatorMock.Object);
        var mockContext = new Mock<FunctionContext>();
        var context = mockContext.Object;
        var request = new FakeHttpRequestData(context);
        request.Headers.Add("x-forwarded-for", "10.0.0.1");
        
        var json = JsonHelper.Serialize(refreshTokensData);
        await using (var writer = new StreamWriter(request.Body, Encoding.UTF8, 1024, leaveOpen: true))
        {
            await writer.WriteAsync(json);
        }
        request.Body.Position = 0;
        
        var response = await function.RefreshTokens(request, context);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        mediatorMock.Verify(m => m.Send(It.IsAny<RefreshTokensRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
