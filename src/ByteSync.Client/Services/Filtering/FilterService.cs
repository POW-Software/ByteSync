using ByteSync.Business.Filtering.Expressions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Filtering;

public class FilterService : IFilterService
{
    private readonly IFilterParser _filterParser;
    private readonly IExpressionEvaluatorFactory _expressionEvaluatorFactory;
    private readonly ILogger<FilterService> _logger;

    public FilterService(IFilterParser filterParser, IExpressionEvaluatorFactory expressionEvaluatorFactory, 
        ILogger<FilterService> logger)
    {
        _filterParser = filterParser;
        _expressionEvaluatorFactory = expressionEvaluatorFactory;
        _logger = logger;
    }
    
    public Func<ComparisonItem, bool> BuildFilter(string filterText)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building filter from text: {FilterText}", filterText);
            return _ => false;
        }
    }

    public Func<ComparisonItem, bool> BuildFilter(List<string> filterTexts)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building filter from multiple texts: {FilterTexts}", string.Join(", ", filterTexts));
            return _ => false;
        }
    }
}