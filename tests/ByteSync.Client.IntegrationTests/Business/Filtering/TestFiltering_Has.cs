using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Has : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    #region has:access-issue

    [Test]
    public void HasAccessIssue_WhenFileIsAccessible_ShouldReturnFalse()
    {
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var filterText = "has:access-issue";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeFalse();
    }

    [Test]
    public void HasAccessIssue_WhenFileIsInaccessible_ShouldReturnTrue()
    {
        var comparisonItem = PrepareComparisonWithInaccessibleFile("A1");
        var filterText = "has:access-issue";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeTrue();
    }

    [Test]
    public void HasAccessIssue_WhenDirectoryIsInaccessible_ShouldReturnTrue()
    {
        var comparisonItem = PrepareComparisonWithInaccessibleDirectory("A1");
        var filterText = "has:access-issue";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeTrue();
    }

    [Test]
    public void HasAccessIssue_WithNotOperator_ShouldReturnInverse()
    {
        var accessibleItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var inaccessibleItem = PrepareComparisonWithInaccessibleFile("A1");
        var filterText = "NOT has:access-issue";

        EvaluateFilterExpression(filterText, accessibleItem).Should().BeTrue();
        EvaluateFilterExpression(filterText, inaccessibleItem).Should().BeFalse();
    }

    [Test]
    public void HasAccessIssue_CaseInsensitive_ShouldWork()
    {
        var comparisonItem = PrepareComparisonWithInaccessibleFile("A1");

        EvaluateFilterExpression("has:access-issue", comparisonItem).Should().BeTrue();
        EvaluateFilterExpression("HAS:Access-Issue", comparisonItem).Should().BeTrue();
        EvaluateFilterExpression("HAS:ACCESS-ISSUE", comparisonItem).Should().BeTrue();
    }

    #endregion

    #region has:computation-error

    [Test]
    public void HasComputationError_WhenNoError_ShouldReturnFalse()
    {
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var filterText = "has:computation-error";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeFalse();
    }

    [Test]
    public void HasComputationError_WhenHasAnalysisError_ShouldReturnTrue()
    {
        var comparisonItem = PrepareComparisonWithAnalysisError("A1");
        var filterText = "has:computation-error";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeTrue();
    }

    [Test]
    public void HasComputationError_WithNotOperator_ShouldReturnInverse()
    {
        var normalItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var errorItem = PrepareComparisonWithAnalysisError("A1");
        var filterText = "NOT has:computation-error";

        EvaluateFilterExpression(filterText, normalItem).Should().BeTrue();
        EvaluateFilterExpression(filterText, errorItem).Should().BeFalse();
    }

    #endregion

    #region has:sync-error

    [Test]
    public void HasSyncError_WhenNoError_ShouldReturnFalse()
    {
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var filterText = "has:sync-error";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeFalse();
    }

    [Test]
    public void HasSyncError_WhenHasSyncError_ShouldReturnTrue()
    {
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        comparisonItem.ItemSynchronizationStatus.IsErrorStatus = true;
        var filterText = "has:sync-error";

        var result = EvaluateFilterExpression(filterText, comparisonItem);

        result.Should().BeTrue();
    }

    [Test]
    public void HasSyncError_WithNotOperator_ShouldReturnInverse()
    {
        var normalItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var errorItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        errorItem.ItemSynchronizationStatus.IsErrorStatus = true;
        var filterText = "NOT has:sync-error";

        EvaluateFilterExpression(filterText, normalItem).Should().BeTrue();
        EvaluateFilterExpression(filterText, errorItem).Should().BeFalse();
    }

    #endregion

    #region Combinations

    [Test]
    public void HasFilters_WithAndOperator_ShouldCombineCorrectly()
    {
        var comparisonItem = PrepareComparisonWithInaccessibleFile("A1");
        comparisonItem.ItemSynchronizationStatus.IsErrorStatus = true;

        EvaluateFilterExpression("has:access-issue AND has:sync-error", comparisonItem).Should().BeTrue();
        EvaluateFilterExpression("has:access-issue AND NOT has:sync-error", comparisonItem).Should().BeFalse();
    }

    [Test]
    public void HasFilters_WithOrOperator_ShouldCombineCorrectly()
    {
        var accessIssueOnly = PrepareComparisonWithInaccessibleFile("A1");
        var syncErrorOnly = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        syncErrorOnly.ItemSynchronizationStatus.IsErrorStatus = true;
        var noError = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);

        var filterText = "has:access-issue OR has:sync-error";

        EvaluateFilterExpression(filterText, accessIssueOnly).Should().BeTrue();
        EvaluateFilterExpression(filterText, syncErrorOnly).Should().BeTrue();
        EvaluateFilterExpression(filterText, noError).Should().BeFalse();
    }

    [Test]
    public void HasFilters_WithIsFileFilter_ShouldCombineCorrectly()
    {
        var inaccessibleFile = PrepareComparisonWithInaccessibleFile("A1");
        var inaccessibleDir = PrepareComparisonWithInaccessibleDirectory("A1");

        var filterText = "has:access-issue AND is:file";

        EvaluateFilterExpression(filterText, inaccessibleFile).Should().BeTrue();
        EvaluateFilterExpression(filterText, inaccessibleDir).Should().BeFalse();
    }

    #endregion

    #region Parser Error Cases

    [Test]
    public void HasFilter_WithoutColon_ShouldReturnIncomplete()
    {
        var filterText = "has access-issue";

        var parseResult = _filterParser.TryParse(filterText);

        parseResult.IsComplete.Should().BeFalse();
    }

    [Test]
    public void HasFilter_WithUnknownType_ShouldReturnIncomplete()
    {
        var filterText = "has:unknown-type";

        var parseResult = _filterParser.TryParse(filterText);

        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().Contain("Unknown has type");
    }

    [Test]
    public void HasFilter_WithEmptyType_ShouldReturnIncomplete()
    {
        var filterText = "has:";

        var parseResult = _filterParser.TryParse(filterText);

        parseResult.IsComplete.Should().BeFalse();
    }

    #endregion
}

