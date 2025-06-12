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

        return expression.ComparisonOperator switch
        {
            ComparisonOperator.Equals => string.Equals(name!, expression.SearchText, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.NotEquals => !string.Equals(name!, expression.SearchText, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.RegexMatch => Regex.IsMatch(name!, expression.SearchText, RegexOptions.IgnoreCase),
            _ => false
        };
        
        // var inventories = item.ContentIdentities
        //     .SelectMany(ci => ci.GetInventories())
        //     .ToHashSet();
        //
        // var inventoryParts = item.ContentIdentities
        //     .SelectMany(ci => ci.GetInventoryParts())
        //     .ToHashSet();
        //
        // List<string> codes = inventories
        //     .Select(i => i.Letter)
        //     .Union(inventoryParts.Select(i => i.Code))
        //     .ToList();
        //
        // return codes.Any(c => c.Equals(expression.DataSource, StringComparison.OrdinalIgnoreCase));
    }
}