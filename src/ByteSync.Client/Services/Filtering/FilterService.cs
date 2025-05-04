using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Filtering;

public class FilterService : IFilterService
{
    private readonly IFilterParser _filterParser;
    private readonly IExpressionEvaluatorFactory _expressionEvaluatorFactory;

    public FilterService(IFilterParser filterParser, IExpressionEvaluatorFactory expressionEvaluatorFactory)
    {
        _filterParser = filterParser;
        _expressionEvaluatorFactory = expressionEvaluatorFactory;
    }
    
    public Func<ComparisonItem, bool> BuildFilter(string filterText)
    {
        try
        {
            var expression = _filterParser.Parse(filterText);

            var evaluator = _expressionEvaluatorFactory.GetEvaluator(expression);

            return item => evaluator.Evaluate(expression, item);
        }
        catch (Exception)
        {
            // If parsing fails, fall back to a simpler approach
            return BuildLegacyFilter(filterText);
        }
    }

    private Func<ComparisonItem, bool> BuildLegacyFilter(string filterText)
    {
        if (string.IsNullOrEmpty(filterText))
        {
            return _ => true;
        }

        return comparisonItem =>
        {
            List<string> expressions = filterText.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var advancedExpressions = expressions.Where(e => e.StartsWith(":")).ToList();
            var otherExpressions = expressions.Where(e => !e.StartsWith(":")).ToList();

            foreach (var advancedExpression in advancedExpressions)
            {
                if (advancedExpression.Equals(":file", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
                    {
                        return false;
                    }
                }

                if (advancedExpression.Equals(":dir", StringComparison.InvariantCultureIgnoreCase)
                    || advancedExpression.Equals(":directory", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (comparisonItem.FileSystemType == FileSystemTypes.File)
                    {
                        return false;
                    }
                }

                if (advancedExpression.StartsWith(":only", StringComparison.InvariantCultureIgnoreCase))
                {
                    var letter = advancedExpression.Substring(":only".Length).ToUpper();

                    var inventories = comparisonItem.ContentIdentities.SelectMany(ci => ci.GetInventories())
                        .ToHashSet();

                    if (inventories.Count != 1 || !inventories.First().Letter.Equals(letter))
                    {
                        return false;
                    }
                }

                if (advancedExpression.StartsWith(":ison", StringComparison.InvariantCultureIgnoreCase))
                {
                    var letter = advancedExpression.Substring(":ison".Length).ToUpper();

                    var inventories = comparisonItem.ContentIdentities.SelectMany(ci => ci.GetInventories())
                        .ToHashSet();

                    if (!inventories.Any(i => i.Letter.Equals(letter)))
                    {
                        return false;
                    }
                }
            }

            if (otherExpressions.Count == 0)
            {
                return true;
            }
            else
            {
                var containsAll = otherExpressions.All(e =>
                    comparisonItem.PathIdentity.FileName.Contains(e, StringComparison.OrdinalIgnoreCase));

                return containsAll;
            }
        };
    }
}