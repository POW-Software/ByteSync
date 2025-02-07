using Autofac;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using Moq;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public class TestContextGenerator
{
    private readonly IContainer _container;

    public TestContextGenerator(IContainer container)
    {
        _container = container;
    }

    public void GenerateSession()
    {
        Mock<ISessionService> mockSessionService = _container.Resolve<Mock<ISessionService>>();
        mockSessionService.Setup(m => m.CurrentSession).Returns(new CloudSession
        {
            SessionId = Guid.NewGuid().ToString(),
            Created = DateTime.UtcNow,
        });
    }

    public ByteSyncEndpoint GenerateCurrentEndpoint()
    {
        var currentEndPoint = new ByteSyncEndpoint
        {
            ClientId = "CI_A",
            ClientInstanceId = "CII_A",
            IpAddress = "localhost",
            OSPlatform = OSPlatforms.Windows
        };
        
        var mockEnvironmentService = _container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.ClientInstanceId).Returns(currentEndPoint.ClientInstanceId);
        
        var mockSessionMemberRepository = _container.Resolve<Mock<ISessionMemberRepository>>();
        mockSessionMemberRepository.Setup(m => m.GetElement("CII_A")).Returns(new SessionMemberInfo
        {
            PrivateData = new SessionMemberPrivateData
            {
                MachineName = "MachineName"
            },
            Endpoint = currentEndPoint,
        });
        
        var mockConnectionService = _container.Resolve<Mock<IConnectionService>>();
        mockConnectionService.Setup(m => m.CurrentEndPoint).Returns(currentEndPoint);

        return currentEndPoint;
    }
}