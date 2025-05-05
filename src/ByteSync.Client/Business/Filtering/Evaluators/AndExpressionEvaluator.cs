using ByteSync.Business.Filtering.Expressions;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class AndExpressionEvaluator : ExpressionEvaluator<AndExpression>
{
    private readonly IExpressionEvaluatorFactory _expressionEvaluatorFactory;

    public AndExpressionEvaluator(IExpressionEvaluatorFactory expressionEvaluatorFactory)
    {
        _expressionEvaluatorFactory = expressionEvaluatorFactory;
    }

    public override bool Evaluate(AndExpression expression, ComparisonItem item)
    {
        var leftEvaluator = _expressionEvaluatorFactory.GetEvaluator(expression.Left);
        var rightEvaluator = _expressionEvaluatorFactory.GetEvaluator(expression.Right);
        
        return leftEvaluator.Evaluate(expression.Left, item) && 
               rightEvaluator.Evaluate(expression.Right, item);
    }
}