using System;
using System.Reflection;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Comparisons;

[TestFixture]
public class SynchronizationRuleMatcherNameTests
{
    [TestCase("file.txt", "file.txt", ConditionOperatorTypes.Equals, true)]
    [TestCase("file.txt", "other.txt", ConditionOperatorTypes.Equals, false)]
    [TestCase("file.txt", "*.txt", ConditionOperatorTypes.Equals, true)]
    [TestCase("file.txt", "*.doc", ConditionOperatorTypes.Equals, false)]
    [TestCase("file.txt", "*.txt", ConditionOperatorTypes.NotEquals, false)]
    [TestCase("file.txt", "*.doc", ConditionOperatorTypes.NotEquals, true)]
    public void ConditionMatchesName_ShouldBehaveAsExpected(string name, string pattern, ConditionOperatorTypes op, bool expected)
    {
        var matcher = new SynchronizationRuleMatcher(new Mock<IAtomicActionConsistencyChecker>().Object,
            new Mock<IAtomicActionRepository>().Object);

        var condition = new AtomicCondition
        {
            Source = new DataPart("A"),
            ComparisonElement = ComparisonElement.Name,
            ConditionOperator = op,
            NamePattern = pattern
        };

        var pathIdentity = new PathIdentity(FileSystemTypes.File, name, name, name);
        var item = new ComparisonItem(pathIdentity);

        var method = typeof(SynchronizationRuleMatcher)
            .GetMethod("ConditionMatchesName", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = (bool)method.Invoke(matcher, new object[] { condition, item })!;
        result.Should().Be(expected);
    }
}
