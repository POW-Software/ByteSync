using ByteSync.Functions.Helpers.Middlewares;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ByteSync.Functions.UnitTests.Helpers.Middlewares;

public class JwtMiddlewareTests
{
    private readonly Mock<IClientsRepository> _mockRepo;
    private readonly Mock<IOptions<AppSettings>> _mockOptions;
    private readonly Mock<ILogger<JwtMiddleware>> _mockLogger;
    private JwtMiddleware _middleware;
    
    public JwtMiddlewareTests()
    {
        _mockRepo = new Mock<IClientsRepository>();
        _mockOptions = new Mock<IOptions<AppSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(new AppSettings { Secret = "un_secret_assez_long_pour_les_tests_unitaires" });
        _mockLogger = new Mock<ILogger<JwtMiddleware>>();
        
        _middleware = new JwtMiddleware(_mockOptions.Object, _mockRepo.Object, _mockLogger.Object);
    }
    
    [Test]
    public async Task Invoke_WithAnonymousEndpoint_ShouldCallNext()
    {
        // Arrange
        var mockContext = new Mock<FunctionContext>();
        var mockDefinition = new Mock<FunctionDefinition>();
        mockDefinition.Setup(d => d.EntryPoint).Returns("ByteSync.Functions.Http.AuthFunction.Login");
        mockContext.Setup(c => c.FunctionDefinition).Returns(mockDefinition.Object);
        
        bool nextWasCalled = false;
        FunctionExecutionDelegate next = _ => {
            nextWasCalled = true;
            return Task.CompletedTask;
        };
        
        // Act
        await _middleware.Invoke(mockContext.Object, next);
        
        // Assert
        nextWasCalled.Should().BeTrue();
    }
}