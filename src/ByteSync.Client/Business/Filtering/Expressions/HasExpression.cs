namespace ByteSync.Business.Filtering.Expressions;

public class HasExpression : FilterExpression
{
    public HasExpressionType ExpressionType { get; }

    public HasExpression(HasExpressionType expressionType)
    {
        ExpressionType = expressionType;
    }
}

