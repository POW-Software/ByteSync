using ByteSync.Business.Actions.Shared;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Sessions.Synchronizations;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class SynchronizationConfirmationViewModelTests : AbstractTester
{
    private Mock<IDataNodeRepository> _dataNodeRepository = null!;
    private Mock<ISessionMemberRepository> _sessionMemberRepository = null!;
    private Mock<IFormatKbSizeConverter> _formatKbSizeConverter = null!;
    private Mock<ILocalizationService> _localizationService = null!;

    [SetUp]
    public void SetUp()
    {
        _dataNodeRepository = new Mock<IDataNodeRepository>();
        _sessionMemberRepository = new Mock<ISessionMemberRepository>();
        _formatKbSizeConverter = new Mock<IFormatKbSizeConverter>();
        _localizationService = new Mock<ILocalizationService>();
        
        _localizationService.Setup(l => l[It.IsAny<string>()]).Returns((string key) => key);
        _formatKbSizeConverter.Setup(c => c.Convert(It.IsAny<long?>())).Returns((long? size) => $"{size} B");
    }

    [Test]
    public void Constructor_ShouldComputeTotalActionsCount()
    {
        var actions = CreateTestActions(5);
        
        var viewModel = CreateViewModel(actions);
        
        viewModel.TotalActionsCount.Should().Be(5);
    }

    [Test]
    public void Constructor_ShouldComputeTotalDataSize()
    {
        var actions = new List<SharedAtomicAction>
        {
            CreateAction("1", size: 1000),
            CreateAction("2", size: 2000),
            CreateAction("3", size: null)
        };
        
        var viewModel = CreateViewModel(actions);
        
        _formatKbSizeConverter.Verify(c => c.Convert(3000L), Times.Once);
    }

    [Test]
    public void Constructor_ShouldGroupActionsByDestination()
    {
        var actions = new List<SharedAtomicAction>
        {
            CreateActionWithTarget("1", "client1", "node1", ActionOperatorTypes.Create),
            CreateActionWithTarget("2", "client1", "node1", ActionOperatorTypes.Delete),
            CreateActionWithTarget("3", "client2", "node2", ActionOperatorTypes.SynchronizeContentOnly)
        };
        
        SetupDataNodes(
            ("client1", "node1", "A"),
            ("client2", "node2", "B"));
        SetupSessionMembers(
            ("client1", "Machine1"),
            ("client2", "Machine2"));
        
        var viewModel = CreateViewModel(actions);
        
        viewModel.DestinationSummaryViewModels.Should().HaveCount(2);
    }

    [Test]
    public void Constructor_ShouldComputeCorrectActionCounts()
    {
        var actions = new List<SharedAtomicAction>
        {
            CreateActionWithTarget("1", "client1", "node1", ActionOperatorTypes.Create),
            CreateActionWithTarget("2", "client1", "node1", ActionOperatorTypes.Create),
            CreateActionWithTarget("3", "client1", "node1", ActionOperatorTypes.SynchronizeContentOnly),
            CreateActionWithTarget("4", "client1", "node1", ActionOperatorTypes.SynchronizeDate),
            CreateActionWithTarget("5", "client1", "node1", ActionOperatorTypes.Delete)
        };
        
        SetupDataNodes(("client1", "node1", "A"));
        SetupSessionMembers(("client1", "TestMachine"));
        
        var viewModel = CreateViewModel(actions);
        
        viewModel.DestinationSummaryViewModels.Should().HaveCount(1);
        var summary = viewModel.DestinationSummaryViewModels[0].Summary;
        summary.CreateCount.Should().Be(2);
        summary.SynchronizeContentCount.Should().Be(1);
        summary.SynchronizeDateCount.Should().Be(1);
        summary.DeleteCount.Should().Be(1);
    }

    [Test]
    public async Task ConfirmCommand_ShouldReturnTrue()
    {
        var actions = CreateTestActions(1);
        var viewModel = CreateViewModel(actions);
        
        var responseTask = viewModel.WaitForResponse();
        viewModel.ConfirmCommand.Execute().Subscribe();
        
        var result = await responseTask;
        
        result.Should().BeTrue();
    }

    [Test]
    public async Task CancelCommand_ShouldReturnFalse()
    {
        var actions = CreateTestActions(1);
        var viewModel = CreateViewModel(actions);
        
        var responseTask = viewModel.WaitForResponse();
        viewModel.CancelCommand.Execute().Subscribe();
        
        var result = await responseTask;
        
        result.Should().BeFalse();
    }

    [Test]
    public async Task CancelIfNeeded_ShouldReturnFalse()
    {
        var actions = CreateTestActions(1);
        var viewModel = CreateViewModel(actions);
        
        var responseTask = viewModel.WaitForResponse();
        await viewModel.CancelIfNeeded();
        
        var result = await responseTask;
        
        result.Should().BeFalse();
    }

    [Test]
    public void DestinationSummaryViewModel_ShouldFormatHeaderText()
    {
        var summary = new DestinationActionsSummary
        {
            DestinationCode = "A",
            MachineName = "TestMachine"
        };
        
        var summaryVm = new DestinationSummaryViewModel(summary, _localizationService.Object);
        
        summaryVm.HeaderText.Should().Contain("A");
        summaryVm.HeaderText.Should().Contain("TestMachine");
    }

    [Test]
    public void DestinationSummaryViewModel_ShouldHaveCorrectVisibilityFlags()
    {
        var summary = new DestinationActionsSummary
        {
            CreateCount = 2,
            SynchronizeContentCount = 0,
            SynchronizeDateCount = 1,
            DeleteCount = 0
        };
        
        var summaryVm = new DestinationSummaryViewModel(summary, _localizationService.Object);
        
        summaryVm.HasCreateActions.Should().BeTrue();
        summaryVm.HasSyncContentActions.Should().BeFalse();
        summaryVm.HasSyncDateActions.Should().BeTrue();
        summaryVm.HasDeleteActions.Should().BeFalse();
    }

    [Test]
    public void DestinationActionsSummary_TotalActionsCount_ShouldSumAllCounts()
    {
        var summary = new DestinationActionsSummary
        {
            CreateCount = 2,
            SynchronizeContentCount = 3,
            SynchronizeDateCount = 1,
            DeleteCount = 4
        };
        
        summary.TotalActionsCount.Should().Be(10);
    }

    [Test]
    public void Constructor_WithEmptyActions_ShouldHaveZeroTotals()
    {
        var actions = new List<SharedAtomicAction>();
        
        var viewModel = CreateViewModel(actions);
        
        viewModel.TotalActionsCount.Should().Be(0);
        viewModel.DestinationSummaryViewModels.Should().BeEmpty();
    }

    [Test]
    public void Constructor_WithActionsWithoutTarget_ShouldNotCrash()
    {
        var actions = new List<SharedAtomicAction>
        {
            new SharedAtomicAction("1") { Target = null }
        };
        
        var viewModel = CreateViewModel(actions);
        
        viewModel.TotalActionsCount.Should().Be(1);
        viewModel.DestinationSummaryViewModels.Should().BeEmpty();
    }

    private SynchronizationConfirmationViewModel CreateViewModel(List<SharedAtomicAction> actions)
    {
        return new SynchronizationConfirmationViewModel(
            actions,
            _dataNodeRepository.Object,
            _sessionMemberRepository.Object,
            _formatKbSizeConverter.Object,
            _localizationService.Object);
    }

    private List<SharedAtomicAction> CreateTestActions(int count)
    {
        var actions = new List<SharedAtomicAction>();
        for (int i = 0; i < count; i++)
        {
            actions.Add(CreateAction(i.ToString()));
        }
        return actions;
    }

    private SharedAtomicAction CreateAction(string id, long? size = null)
    {
        return new SharedAtomicAction(id)
        {
            Size = size,
            Operator = ActionOperatorTypes.DoNothing
        };
    }

    private SharedAtomicAction CreateActionWithTarget(string id, string clientInstanceId, string nodeId, ActionOperatorTypes operatorType)
    {
        return new SharedAtomicAction(id)
        {
            Target = new SharedDataPart
            {
                ClientInstanceId = clientInstanceId,
                NodeId = nodeId
            },
            Operator = operatorType
        };
    }

    private void SetupDataNodes(params (string clientInstanceId, string nodeId, string code)[] nodes)
    {
        foreach (var (clientInstanceId, nodeId, code) in nodes)
        {
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = nodeId, ClientInstanceId = clientInstanceId, Code = code }
            };
            _dataNodeRepository
                .Setup(r => r.GetDataNodesByClientInstanceId(clientInstanceId))
                .Returns(dataNodes);
        }
    }

    private void SetupSessionMembers(params (string clientInstanceId, string machineName)[] members)
    {
        var sessionMembers = members.Select(m => new SessionMember
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = m.clientInstanceId },
            PrivateData = new SessionMemberPrivateData { MachineName = m.machineName }
        }).ToList();
        
        _sessionMemberRepository.SetupGet(r => r.Elements).Returns(sessionMembers);
    }
}
