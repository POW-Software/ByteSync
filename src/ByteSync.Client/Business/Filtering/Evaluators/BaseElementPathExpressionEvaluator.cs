using System.Text.RegularExpressions;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public abstract class BaseElementPathExpressionEvaluator<T> : ExpressionEvaluator<BaseElementPathExpression>
    where T : BaseElementPathExpression
{
    protected abstract string GetTargetValue(ComparisonItem item);

    protected abstract string ImproveSearchText(string searchText, ComparisonOperator comparisonOperator);
    
    public override bool Evaluate(BaseElementPathExpression expression, ComparisonItem item)
    {
        var targetValue = GetTargetValue(item);

        if (targetValue.IsNullOrEmpty())
        {
            return false;
        }

        var comparisonOperator = expression.ComparisonOperator;
        var searchText = expression.SearchText;
        
        searchText = ImproveSearchText(searchText, comparisonOperator);

        if (searchText.Contains("*") && comparisonOperator.In(ComparisonOperator.Equals, ComparisonOperator.NotEquals))
        {
            comparisonOperator = comparisonOperator == ComparisonOperator.Equals
                ? ComparisonOperator.RegexMatch
                : ComparisonOperator.RegexNotMatch;

            searchText = "^" + Regex.Escape(expression.SearchText).Replace("\\*", ".*") + "$";
        }
        
        var regex = new Regex(
            searchText,
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(200) 
        );
        
        // https://stackoverflow.com/questions/3968845/format-string-with-dashes
        return comparisonOperator switch
        {
            ComparisonOperator.Equals => string.Equals(targetValue, searchText, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.NotEquals => !string.Equals(targetValue, searchText, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.RegexMatch => regex.IsMatch(targetValue),
            ComparisonOperator.RegexNotMatch => !regex.IsMatch(targetValue),
            _ => false
        };
    }
}