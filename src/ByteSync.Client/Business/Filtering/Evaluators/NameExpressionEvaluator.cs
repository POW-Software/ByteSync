using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class NameExpressionEvaluator : ExpressionEvaluator<NameExpression>
{
    public override bool Evaluate(NameExpression expression, ComparisonItem item)
    {
        var name = item?.PathIdentity.FileName;
        
        if (name.IsNullOrEmpty())
        {
            return false;
        }
        
        var comparisonOperator = expression.ComparisonOperator;
        var searchText = expression.SearchText;

        if (searchText.Contains("*") && comparisonOperator.In(ComparisonOperator.Equals, ComparisonOperator.NotEquals))
        {
            comparisonOperator = ComparisonOperator.RegexMatch;
            searchText = "^" + Regex.Escape(expression.SearchText).Replace("\\*", ".*") + "$";
        }

        return comparisonOperator switch
        {
            ComparisonOperator.Equals => string.Equals(name!, searchText, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.NotEquals => !string.Equals(name!, searchText, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.RegexMatch => Regex.IsMatch(name!, searchText, RegexOptions.IgnoreCase),
            _ => false
        };
    }
}