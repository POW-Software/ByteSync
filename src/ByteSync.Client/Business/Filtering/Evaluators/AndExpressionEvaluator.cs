using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Filtering.Evaluators;

public class AndExpressionEvaluator : ExpressionEvaluator<AndExpression>
{
    private readonly ExpressionEvaluatorFactory _evaluatorFactory;

    public AndExpressionEvaluator(ExpressionEvaluatorFactory evaluatorFactory)
    {
        _evaluatorFactory = evaluatorFactory;
    }

    public override bool Evaluate(AndExpression expression, ComparisonItem item)
    {
        var leftEvaluator = _evaluatorFactory.GetEvaluator(expression.Left);
        var rightEvaluator = _evaluatorFactory.GetEvaluator(expression.Right);
        
        return leftEvaluator.Evaluate(expression.Left, item) && 
               rightEvaluator.Evaluate(expression.Right, item);
    }
}