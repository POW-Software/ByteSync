namespace ByteSync.Business.Filtering.Parsing;

public enum FilterTokenType
{
    None,
    Identifier,
    Operator,
    String,
    Number,
    OpenParenthesis,
    CloseParenthesis,
    LogicalOperator,
    Dot,
    Colon,
    End
}