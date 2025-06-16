using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class PathExpressionEvaluator : BaseElementPathExpressionEvaluator<PathExpression>
{
    protected override string GetTargetValue(ComparisonItem item)
    {
        return item?.PathIdentity.LinkingData ?? string.Empty;
    }

    protected override string ImproveSearchText(string searchText, ComparisonOperator comparisonOperator)
    {
        if (!searchText.Contains("*") && comparisonOperator == ComparisonOperator.Equals)
        {
            searchText = "/" + searchText.TrimStart('\\').TrimStart('/');
        }

        return searchText;
    }
}