using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Evaluators;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Filtering.Values;
using ByteSync.Models.Comparisons.Result;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Business.Filtering;

public class PropertyComparisonExpressionEvaluatorTests
{
	private static ComparisonItem CreateItem()
	{
		var path = new ByteSync.Business.Inventories.PathIdentity(ByteSync.Common.Business.Inventories.FileSystemTypes.File, "/f.txt", "f.txt", "/f.txt");
		return new ComparisonItem(path);
	}

	private static PropertyComparisonExpressionEvaluator CreateEvaluatorReturning(PropertyValueCollection source)
	{
		var extractor = new Mock<ByteSync.Interfaces.Services.Filtering.IPropertyValueExtractor>();
		extractor.Setup(x => x.GetPropertyValue(It.IsAny<ComparisonItem>(), It.IsAny<DataPart?>(), It.IsAny<string>()))
			.Returns(source);

		var comparer = new PropertyComparer();
		return new PropertyComparisonExpressionEvaluator(extractor.Object, comparer);
	}

	private static PropertyValueCollection Strings(params string[] values)
	{
		return new PropertyValueCollection(values.Select(v => new PropertyValue(v)));
	}

	private static PropertyValueCollection Numbers(params long[] values)
	{
		return new PropertyValueCollection(values.Select(v => new PropertyValue(v)));
	}

	private static PropertyValueCollection Dates(params DateTime[] values)
	{
		return new PropertyValueCollection(values.Select(v => new PropertyValue(v)));
	}

	[Test]
	public void CompareWithLiteral_RegexMatch_ValidPattern_ReturnsTrueOnMatch()
	{
		var evaluator = CreateEvaluatorReturning(Strings("file123.txt", "other"));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), "name", ComparisonOperator.RegexMatch, null, "file\\d+\\.txt");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeTrue();
	}

	[Test]
	public void CompareWithLiteral_RegexMatch_InvalidPattern_ReturnsFalse()
	{
		var evaluator = CreateEvaluatorReturning(Strings("file123.txt"));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), "name", ComparisonOperator.RegexMatch, null, "[");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeFalse();
	}

	[Test]
	public void CompareWithLiteral_Size_WithValidUnit_ParsesAndCompares()
	{
		var evaluator = CreateEvaluatorReturning(Numbers(2048));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_SIZE, ComparisonOperator.Equals, null, "2KB");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeTrue();
	}

	[Test]
	public void CompareWithLiteral_Size_WithInvalidUnit_CreatesEmptyTargetValues_ThusEqualsIsFalse()
	{
		var evaluator = CreateEvaluatorReturning(Numbers(2048));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_SIZE, ComparisonOperator.Equals, null, "12XB");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeFalse();
	}

	[Test]
	public void CompareWithLiteral_Size_PlainNumber_ParsesAndCompares()
	{
		var evaluator = CreateEvaluatorReturning(Numbers(2048));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_SIZE, ComparisonOperator.GreaterThan, null, "1024");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeTrue();
	}

	[Test]
	public void CompareWithLiteral_Size_PlainNumberInvalid_TargetEmpty_EqualsIsFalse()
	{
		var evaluator = CreateEvaluatorReturning(Numbers(2048));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_SIZE, ComparisonOperator.Equals, null, "abc");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeFalse();
	}

	[Test]
	public void CompareWithLiteral_LastWriteTime_WithNowDuration_ParsesAndCompares()
	{
		var now = DateTime.UtcNow;
		var evaluator = CreateEvaluatorReturning(Dates(now));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_LAST_WRITE_TIME, ComparisonOperator.GreaterThan, null, "now-1m");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeTrue();
	}

	[Test]
	public void CompareWithLiteral_LastWriteTime_InvalidNowDuration_ReturnsFalse()
	{
		var evaluator = CreateEvaluatorReturning(Dates(DateTime.UtcNow));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_LAST_WRITE_TIME, ComparisonOperator.Equals, null, "now-10x");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeFalse();
	}

	[Test]
	public void CompareWithLiteral_LastWriteTime_WithExactDate_ParsesAndCompares()
	{
		var date = new DateTime(2024, 12, 31);
		var evaluator = CreateEvaluatorReturning(Dates(date));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_LAST_WRITE_TIME, ComparisonOperator.Equals, null, "2024-12-31");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeTrue();
	}

	[Test]
	public void CompareWithLiteral_LastWriteTime_WithExactDateTime_ParsesAndCompares()
	{
		var date = new DateTime(2025, 01, 02, 03, 04, 05);
		var evaluator = CreateEvaluatorReturning(Dates(date));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_LAST_WRITE_TIME, ComparisonOperator.Equals, null, "2025-01-02-03-04-05");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeTrue();
	}

	[Test]
	public void CompareWithLiteral_LastWriteTime_InvalidDate_ReturnsFalse()
	{
		var evaluator = CreateEvaluatorReturning(Dates(DateTime.UtcNow));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), Identifiers.PROPERTY_LAST_WRITE_TIME, ComparisonOperator.Equals, null, "2024/01/02");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeFalse();
	}

	[Test]
	public void CompareWithLiteral_UnknownProperty_ReturnsFalse()
	{
		var evaluator = CreateEvaluatorReturning(Strings("any"));
		var expr = new PropertyComparisonExpression(new DataPart("A1"), "unknown-property", ComparisonOperator.Equals, null, "anything");

		var result = evaluator.Evaluate(expr, CreateItem());

		result.Should().BeFalse();
	}
}


