using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons.ConditionMatchers;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherNameTests
{
    private NameConditionMatcher _matcher;
    
    [SetUp]
    public void SetUp()
    {
        _matcher = new NameConditionMatcher();
    }
    
    [TestCase("file.txt", "file.txt", ConditionOperatorTypes.Equals, true)]
    [TestCase("file.txt", "other.txt", ConditionOperatorTypes.Equals, false)]
    [TestCase("file.txt", "*.txt", ConditionOperatorTypes.Equals, true)]
    [TestCase("file.txt", "*.doc", ConditionOperatorTypes.Equals, false)]
    [TestCase("file.txt", "*.txt", ConditionOperatorTypes.NotEquals, false)]
    [TestCase("file.txt", "*.doc", ConditionOperatorTypes.NotEquals, true)]
    public void ConditionMatchesName_ShouldBehaveAsExpected(string name, string pattern, ConditionOperatorTypes op, bool expected)
    {
        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonProperty = ComparisonProperty.Name,
            ConditionOperator = op,
            NamePattern = pattern
        };
        
        var pathIdentity = new PathIdentity(FileSystemTypes.File, name, name, name);
        var item = new ComparisonItem(pathIdentity);
        
        var result = _matcher.Matches(condition, item);
        result.Should().Be(expected);
    }
}