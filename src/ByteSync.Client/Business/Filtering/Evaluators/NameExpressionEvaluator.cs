using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class NameExpressionEvaluator : BaseElementPathExpressionEvaluator<NameExpression>
{
    protected override string GetTargetValue(ComparisonItem item)
    {
        return item?.PathIdentity.FileName ?? string.Empty;
    }

    protected override string ImproveSearchText(string searchText, ComparisonOperator comparisonOperator)
    {
        return searchText;
    }
}