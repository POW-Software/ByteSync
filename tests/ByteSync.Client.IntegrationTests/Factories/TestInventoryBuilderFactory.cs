using Autofac;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Misc;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Factories;

public class TestInventoryBuilderFactory : IntegrationTest
{
    private InventoryBuilderFactory _inventoryBuilderFactor;

    [SetUp]
    public void Setup()
    {
        RegisterType<InventoryBuilderFactory>();
        BuildMoqContainer();
        
        _inventoryBuilderFactor = Container.Resolve<InventoryBuilderFactory>();
    }
    
    [Test]
    public void CreateInventoryBuilder_ShouldBuildInventoryWithCorrectParts()
    {
        // Arrange
        var mockSessionMemberRepo = Container.Resolve<Mock<ISessionMemberRepository>>();
        var mockSessionService = Container.Resolve<Mock<ISessionService>>();
        var mockInventoryService = Container.Resolve<Mock<IInventoryService>>();
        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        var mockPathItemRepository = Container.Resolve<Mock<IDataSourceRepository>>();
        
        var fakeSessionMember = new SessionMemberInfo();
        var fakeSessionSettings = new SessionSettings();
        var fakeProcessData = new InventoryProcessData();
        var fakePlatform = OSPlatforms.Windows;
        var fakePathItems = new List<DataSource> { Mock.Of<DataSource>(), Mock.Of<DataSource>() };
        
        mockSessionMemberRepo.Setup(r => r.GetCurrentSessionMember()).Returns(fakeSessionMember).Verifiable();
        mockSessionService.SetupGet(s => s.CurrentSessionSettings).Returns(fakeSessionSettings).Verifiable();
        mockInventoryService.SetupGet(s => s.InventoryProcessData).Returns(fakeProcessData).Verifiable();
        mockEnvironmentService.SetupGet(e => e.OSPlatform).Returns(fakePlatform).Verifiable();
        mockPathItemRepository.SetupGet(r => r.SortedCurrentMemberPathItems).Returns(fakePathItems).Verifiable();

        // Act
        var inventoryBuilder = _inventoryBuilderFactor.CreateInventoryBuilder();

        // Assert
        inventoryBuilder.Should().NotBeNull();
        
        mockSessionMemberRepo.Verify();
        mockSessionService.Verify();
        mockInventoryService.Verify();
        mockEnvironmentService.Verify();
        mockPathItemRepository.Verify();
    }
}