using ByteSync.Business.Filtering.Evaluators;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Filtering.Values;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Filtering;

public class PropertyComparerTests
{
    private static PropertyValueCollection Strings(params string[] values)
    {
        return new PropertyValueCollection(values.Select(v => new PropertyValue(v)));
    }
    
    [Test]
    public void CompareStrings_Equals_IgnoresCase()
    {
        var c1 = Strings("abc");
        var c2 = Strings("ABC");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.Equals);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_NotEquals_DifferentStrings()
    {
        var c1 = Strings("foo");
        var c2 = Strings("bar");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.NotEquals);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_GreaterThan_ShouldBeTrueWhenLeftIsLexicographicallyAfterRight_IgnoringCase()
    {
        var c1 = Strings("b");
        var c2 = Strings("a");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.GreaterThan);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_LessThan_ShouldBeTrueWhenLeftIsLexicographicallyBeforeRight_IgnoringCase()
    {
        var c1 = Strings("a");
        var c2 = Strings("b");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.LessThan);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_GreaterThanOrEqual_ShouldBeTrueWhenEqual_IgnoringCase()
    {
        var c1 = Strings("Test");
        var c2 = Strings("test");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.GreaterThanOrEqual);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_LessThanOrEqual_ShouldBeTrueWhenEqual_IgnoringCase()
    {
        var c1 = Strings("Same");
        var c2 = Strings("same");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.LessThanOrEqual);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_RegexMatch_ShouldReturnTrueOnRegexMatch()
    {
        var c1 = Strings("file123.txt");
        var c2 = Strings("file\\d+\\.txt");
        
        var comparer = new PropertyComparer();
        var result = comparer.CompareValues(c1, c2, ComparisonOperator.RegexMatch);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void CompareStrings_UnsupportedOperator_ShouldThrow()
    {
        var c1 = Strings("abc");
        var c2 = Strings("abc");
        
        var comparer = new PropertyComparer();
        var act = () => comparer.CompareValues(c1, c2, ComparisonOperator.RegexNotMatch);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported string operator*");
    }
}