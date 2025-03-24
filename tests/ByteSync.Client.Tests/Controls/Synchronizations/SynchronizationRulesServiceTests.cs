using Moq;
using NUnit.Framework;
using FluentAssertions;
using ByteSync.Services.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Business.Actions.Loose;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Profiles;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Tests.Controls.Synchronizations;

[TestFixture]
public class SynchronizationRulesServiceTests
{
    private Mock<ISynchronizationRuleRepository> _ruleRepositoryMock;
    private Mock<IDataPartIndexer> _dataPartIndexerMock;
    private Mock<ISynchronizationRuleMatcher> _ruleMatcherMock;
    private Mock<IComparisonItemRepository> _comparisonItemRepositoryMock;
    private Mock<ISynchronizationRulesConverter> _rulesConverterMock;
    private SynchronizationRulesService _service;

    [SetUp]
    public void SetUp()
    {
        _ruleRepositoryMock = new Mock<ISynchronizationRuleRepository>();
        _dataPartIndexerMock = new Mock<IDataPartIndexer>();
        _ruleMatcherMock = new Mock<ISynchronizationRuleMatcher>();
        _comparisonItemRepositoryMock = new Mock<IComparisonItemRepository>();
        _rulesConverterMock = new Mock<ISynchronizationRulesConverter>();

        _service = new SynchronizationRulesService(
            _ruleRepositoryMock.Object,
            _dataPartIndexerMock.Object,
            _ruleMatcherMock.Object,
            _comparisonItemRepositoryMock.Object,
            _rulesConverterMock.Object
        );
    }

    [Test]
    public void AddOrUpdateSynchronizationRule_ShouldRefreshAndAdd()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);

        _service.AddOrUpdateSynchronizationRule(rule);

        _ruleRepositoryMock.Verify(r => r.AddOrUpdate(rule), Times.Once);
        _dataPartIndexerMock.Verify(d => d.Remap(It.IsAny<HashSet<SynchronizationRule>>()), Times.Once);
        _ruleMatcherMock.Verify(m => m.MakeMatches(It.IsAny<List<ComparisonItem>>(), It.IsAny<HashSet<SynchronizationRule>>()), Times.Once);
    }

    [Test]
    public void Remove_ShouldCallRepositoryAndRefresh()
    {
        var rule = new SynchronizationRule(FileSystemTypes.File, ConditionModes.All);

        _service.Remove(rule);

        _ruleRepositoryMock.Verify(r => r.Remove(rule), Times.Once);
        _dataPartIndexerMock.Verify(d => d.Remap(It.IsAny<HashSet<SynchronizationRule>>()), Times.Once);
    }

    [Test]
    public void Clear_ShouldEmptyRepositoryAndRefresh()
    {
        _service.Clear();

        _ruleRepositoryMock.Verify(r => r.Clear(), Times.Once);
        _dataPartIndexerMock.Verify(d => d.Remap(It.IsAny<HashSet<SynchronizationRule>>()), Times.Once);
    }

    [Test]
    public void GetLooseSynchronizationRules_ShouldReturnConvertedRules()
    {
        var rules = new List<SynchronizationRule>();
        _ruleRepositoryMock.Setup(r => r.Elements).Returns(rules);

        var looseRules = new List<LooseSynchronizationRule>();
        _rulesConverterMock
            .Setup(c => c.ConvertLooseSynchronizationRules(rules))
            .Returns(looseRules);

        var result = _service.GetLooseSynchronizationRules();

        result.Should().BeSameAs(looseRules);
    }
}