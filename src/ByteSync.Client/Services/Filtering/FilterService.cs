using ByteSync.Business.Filtering.Expressions;
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
        var parseResult = _filterParser.TryParse(filterText);
        
        if (!parseResult.IsComplete)
        {
            // Return a filter that accepts everything if the parsing is incomplete
            // This allows for a more user-friendly experience during typing
            return _ => true;
        }

        var expression = parseResult.Expression!;
        var evaluator = _expressionEvaluatorFactory.GetEvaluator(expression);
        
        return item => evaluator.Evaluate(expression, item);
    }

    public Func<ComparisonItem, bool> BuildFilter(List<string> filterTexts)
    {
        FilterExpression compositeExpression = new TrueExpression();
        
        foreach (var filterText in filterTexts)
        {
            var parseResult = _filterParser.TryParse(filterText);

            if (parseResult.IsComplete)
            {
                compositeExpression = new AndExpression(compositeExpression, parseResult.Expression!);
            }
        }
        
        var evaluator = _expressionEvaluatorFactory.GetEvaluator(compositeExpression);
        
        return item => evaluator.Evaluate(compositeExpression, item);
    }
}