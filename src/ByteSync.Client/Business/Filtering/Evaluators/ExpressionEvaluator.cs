using ByteSync.Business.Filtering.Expressions;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public abstract class ExpressionEvaluator<T> : IExpressionEvaluator<T> where T : FilterExpression
{
    public abstract bool Evaluate(T expression, ComparisonItem item);
    
    // Implementation of non-generic interface
    public bool Evaluate(FilterExpression expression, ComparisonItem item)
    {
        if (expression is T typedExpression)
        {
            return Evaluate(typedExpression, item);
        }
        
        throw new ArgumentException($"Expression is not of type {typeof(T).Name}");
    }
}