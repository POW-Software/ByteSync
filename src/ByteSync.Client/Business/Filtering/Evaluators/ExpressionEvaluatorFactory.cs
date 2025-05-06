using Autofac;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Interfaces.Services.Filtering;

namespace ByteSync.Business.Filtering.Evaluators;

public class ExpressionEvaluatorFactory : IExpressionEvaluatorFactory
{
    private readonly IComponentContext _context;
    private readonly Dictionary<Type, Type> _evaluatorTypes;

    public ExpressionEvaluatorFactory(IComponentContext context)
    {
        _context = context;
        
        _evaluatorTypes = new Dictionary<Type, Type>
        {
            { typeof(AndExpression), typeof(AndExpressionEvaluator) },
            { typeof(OrExpression), typeof(OrExpressionEvaluator) },
            { typeof(NotExpression), typeof(NotExpressionEvaluator) },
            { typeof(TrueExpression), typeof(TrueExpressionEvaluator) },
            { typeof(ExistsExpression), typeof(ExistsExpressionEvaluator) },
            { typeof(FileSystemTypeExpression), typeof(FileSystemTypeExpressionEvaluator) },
            { typeof(FutureStateExpression), typeof(FutureStateExpressionEvaluator) },
            { typeof(OnlyExpression), typeof(OnlyExpressionEvaluator) },
            { typeof(PropertyComparisonExpression), typeof(PropertyComparisonExpressionEvaluator) },
            { typeof(TextSearchExpression), typeof(TextSearchExpressionEvaluator) }
        };
    }

    public IExpressionEvaluator GetEvaluator(FilterExpression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var expressionType = expression.GetType();
        
        // Search for an exact match
        if (_evaluatorTypes.TryGetValue(expressionType, out var evaluatorType))
        {
            return (IExpressionEvaluator)_context.Resolve(evaluatorType);
        }
        
        // Search for a compatible evaluator (handles inheritance)
        foreach (var (exprType, evalType) in _evaluatorTypes)
        {
            if (exprType.IsAssignableFrom(expressionType))
            {
                return (IExpressionEvaluator)_context.Resolve(evalType);
            }
        }
        
        throw new ArgumentException($"No evaluators registered for this type of expression: {expressionType.Name}");
    }
}