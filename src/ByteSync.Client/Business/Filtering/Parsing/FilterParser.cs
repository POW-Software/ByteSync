using System.Text;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Business.Filtering.Parsing;

public class FilterParser : IFilterParser
{
    private readonly IDataPartIndexer _dataPartIndexer;
    private readonly IOperatorParser _operatorParser;
    private readonly IFilterTokenizer _tokenizer;
    
    private FilterToken? CurrentToken { get; set; }

    public FilterParser(IDataPartIndexer dataPartIndexer, IOperatorParser operatorParser, IFilterTokenizer tokenizer)
    {
        _dataPartIndexer = dataPartIndexer;
        _operatorParser = operatorParser;
        _tokenizer = tokenizer;
    }

    public ParseResult TryParse(string filterText)
    {
        _tokenizer.Initialize(filterText ?? string.Empty);
        CurrentToken = null;
        NextToken();
        
        if (string.IsNullOrWhiteSpace(filterText))
            return ParseResult.Success(new TrueExpression());

        // Split by whitespace for simple text search
        var terms = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Check if there are any special expressions
        if (!terms.Any(t => t.Contains(":") || t.Contains(".") || t.Contains("(") ||
                            t.StartsWith(Identifiers.OPERATOR_ACTIONS, StringComparison.OrdinalIgnoreCase) ||
                            t.StartsWith(Identifiers.OPERATOR_NAME, StringComparison.OrdinalIgnoreCase) ||
                            t.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                            t.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                            t.Equals("NOT", StringComparison.OrdinalIgnoreCase)))
        {
            // Simple text search
            FilterExpression compositeExpression = new TrueExpression();
            foreach (var term in terms)
            {
                var textExpression = new TextSearchExpression(term);
                compositeExpression = new AndExpression(compositeExpression, textExpression);
            }

            return ParseResult.Success(compositeExpression);
        }

        // Otherwise, parse the expression
        return TryParseExpression();
    }

    private ParseResult TryParseExpression()
    {
        var leftResult = TryParseTerm();
        if (!leftResult.IsComplete)
        {
            return leftResult;
        }

        var left = leftResult.Expression!;

        while (CurrentToken?.Type == FilterTokenType.LogicalOperator &&
               (CurrentToken.Token.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                CurrentToken.Token.Equals("||", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var rightResult = TryParseTerm();
            if (!rightResult.IsComplete)
            {
                return ParseResult.Incomplete($"Incomplete right operand for OR expression: {rightResult.ErrorMessage}");
            }

            left = new OrExpression(left, rightResult.Expression!);
        }

        return ParseResult.Success(left);
    }

    private ParseResult TryParseTerm()
    {
        var leftResult = TryParseFactor();
        if (!leftResult.IsComplete)
        {
            return leftResult;
        }

        var left = leftResult.Expression!;

        while (CurrentToken?.Type == FilterTokenType.LogicalOperator &&
               (CurrentToken.Token.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                CurrentToken.Token.Equals("&&", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var rightResult = TryParseFactor();
            if (!rightResult.IsComplete)
                return ParseResult.Incomplete($"Incomplete right operand for AND expression: {rightResult.ErrorMessage}");

            left = new AndExpression(left, rightResult.Expression!);
        }

        return ParseResult.Success(left);
    }

    private ParseResult TryParseFactor()
    {
        if (CurrentToken?.Type == FilterTokenType.LogicalOperator &&
            (CurrentToken.Token.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
             CurrentToken.Token.Equals("!", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
    
            // Check if we've reached the end of input after consuming NOT/!
            if (CurrentToken?.Type == FilterTokenType.End)
            {
                return ParseResult.Incomplete("Incomplete expression after NOT: expected an expression to negate");
            }
    
            var expressionResult = TryParseFactor();
            if (!expressionResult.IsComplete)
            {
                return ParseResult.Incomplete($"Incomplete expression after NOT: {expressionResult.ErrorMessage}");
            }
    
            return ParseResult.Success(new NotExpression(expressionResult.Expression!));
        }

        if (CurrentToken?.Type == FilterTokenType.OpenParenthesis)
        {
            NextToken();
            var expressionResult = TryParseExpression();
            if (!expressionResult.IsComplete)
            {
                return ParseResult.Incomplete($"Incomplete expression in parentheses: {expressionResult.ErrorMessage}");
            }

            if (CurrentToken?.Type != FilterTokenType.CloseParenthesis)
            {
                return ParseResult.Incomplete("Expected closing parenthesis");
            }

            NextToken();
            
            return ParseResult.Success(expressionResult.Expression!);
        }

        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals("wb", StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Colon)
            {
                return ParseResult.Incomplete("Expected colon after 'wb'");
            }

            NextToken();
            var baseExpressionResult = TryParseFactor();
            if (!baseExpressionResult.IsComplete)
                return ParseResult.Incomplete($"Incomplete expression after wb:: {baseExpressionResult.ErrorMessage}");
            
            return ParseResult.Success(new FutureStateExpression(baseExpressionResult.Expression!));
        }

        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals(Identifiers.OPERATOR_ON, StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Colon)
            {
                return ParseResult.Incomplete($"Expected colon after '{Identifiers.OPERATOR_ON}'");
            }

            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Identifier)
            {
                return ParseResult.Incomplete($"Expected data source identifier after '{Identifiers.OPERATOR_ON}:'");
            }

            var dataSource = CurrentToken?.Token;
            NextToken();
            
            return ParseResult.Success(new ExistsExpression(dataSource));
        }
        
        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals(Identifiers.OPERATOR_ONLY, StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Colon)
            {
                return ParseResult.Incomplete($"Expected colon after '{Identifiers.OPERATOR_ONLY}'");
            }

            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Identifier)
            {
                return ParseResult.Incomplete($"Expected data source identifier after '{Identifiers.OPERATOR_ONLY}:'");
            }

            var dataSource = CurrentToken?.Token;
            NextToken();
            
            return ParseResult.Success(new OnlyExpression(dataSource));
        }
        
        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals(Identifiers.OPERATOR_IS, StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Colon)
            {
                return ParseResult.Incomplete($"Expected colon after '{Identifiers.OPERATOR_IS}'");
            }

            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Identifier)
            {
                return ParseResult.Incomplete($"Expected file type identifier after '{Identifiers.OPERATOR_IS}:'");
            }

            var typeIdentifier = CurrentToken?.Token.ToLowerInvariant();
            NextToken();
    
            if (typeIdentifier == Identifiers.PROPERTY_FILE)
            {
                return ParseResult.Success(new FileSystemTypeExpression(FileSystemTypes.File));
            }
            else if (typeIdentifier == Identifiers.PROPERTY_DIR || typeIdentifier == Identifiers.PROPERTY_DIRECTORY)
            {
                return ParseResult.Success(new FileSystemTypeExpression(FileSystemTypes.Directory));
            }
            else
            {
                return ParseResult.Incomplete($"Unknown file type: {typeIdentifier}");
            }
        }
        
        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals(Identifiers.OPERATOR_NAME, StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Operator && CurrentToken?.Type != FilterTokenType.Colon)
            {
                return ParseResult.Incomplete($"Expected operator after '{Identifiers.OPERATOR_NAME}'");
            }
            var comparisonOperator = 
                CurrentToken.Type == FilterTokenType.Colon 
                    ? ComparisonOperator.Equals
                    : _operatorParser.Parse(CurrentToken.Token);

            StringBuilder searchText = new();
            NextToken();
            
            while (CurrentToken?.Type == FilterTokenType.String || CurrentToken?.Type == FilterTokenType.Dot || CurrentToken?.Type == FilterTokenType.Identifier)
            {
                searchText.Append(CurrentToken?.Token);
                NextToken();
            }
            
            var nameExpression = new NameExpression(searchText.ToString(), comparisonOperator);
            
            return ParseResult.Success(nameExpression); 
        }
        
        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals(Identifiers.OPERATOR_ACTIONS, StringComparison.OrdinalIgnoreCase))
        {
            string actionPath = CurrentToken?.Token.ToLowerInvariant();
            NextToken();
            
            while (CurrentToken?.Type == FilterTokenType.Dot)
            {
                NextToken();
                if (CurrentToken?.Type != FilterTokenType.Identifier)
                {
                    return ParseResult.Incomplete("Expected identifier after dot in action path");
                }
        
                actionPath += "." + CurrentToken?.Token.ToLowerInvariant();
                NextToken();
            }
            
            if (CurrentToken?.Type == FilterTokenType.End || CurrentToken?.Type == FilterTokenType.LogicalOperator)
            {
                return ParseResult.Success(new ActionComparisonExpression(actionPath, ComparisonOperator.GreaterThan, 0));
            }
            else
            {
                if (CurrentToken?.Type != FilterTokenType.Operator)
                {
                    return ParseResult.Incomplete("Expected operator after action path");
                }
    
                var op = CurrentToken?.Token;
                NextToken();
    
                try {
                    var comparisonOperator = _operatorParser.Parse(op);
        
                    if (CurrentToken?.Type != FilterTokenType.Number && CurrentToken?.Type != FilterTokenType.DateTime)
                    {
                        return ParseResult.Incomplete("Expected numeric value / dateTime after operator in action comparison");
                    }
        
                    if (!int.TryParse(CurrentToken?.Token, out int value))
                    {
                        return ParseResult.Incomplete("Invalid numeric value in action comparison");
                    }
        
                    NextToken();
        
                    return ParseResult.Success(new ActionComparisonExpression(actionPath, comparisonOperator, value));
                }
                catch (ArgumentException ex)
                {
                    return ParseResult.Incomplete(ex.Message);
                }
            }
        }

        if (CurrentToken?.Type == FilterTokenType.Identifier)
        {
            var identifier = CurrentToken.Token;
            NextToken();

            if (CurrentToken?.Type == FilterTokenType.Dot)
            {
                NextToken();
                if (CurrentToken?.Type != FilterTokenType.Identifier)
                {
                    return ParseResult.Incomplete("Expected property name after dot");
                }

                var property = CurrentToken?.Token!;
                NextToken();

                if (CurrentToken?.Type != FilterTokenType.Operator)
                {
                    return ParseResult.Incomplete("Expected operator after property name");
                }

                var op = CurrentToken?.Token;
                NextToken();

                try
                {
                    var filterOperator = _operatorParser.Parse(op);

                    var leftDataPart = _dataPartIndexer.GetDataPart(identifier.ToUpper());
                    if (leftDataPart == null)
                    {
                        return ParseResult.Incomplete($"Unknown data part: {identifier}");
                    }

                    // Check if the right side is a data source or a value
                    if (CurrentToken?.Type == FilterTokenType.Identifier)
                    {
                        var rightIdentifier = CurrentToken.Token;
                        NextToken();

                        if (CurrentToken?.Type == FilterTokenType.Dot)
                        {
                            var rightDataPart = _dataPartIndexer.GetDataPart(rightIdentifier.ToUpper());
                            if (rightDataPart == null)
                            {
                                return ParseResult.Incomplete($"Unknown data part: {rightIdentifier}");
                            }
                            
                            // This is a comparison between two properties
                            NextToken();
                            if (CurrentToken?.Type != FilterTokenType.Identifier && CurrentToken?.Token != "_")
                            {
                                return ParseResult.Incomplete("Expected property name after dot");
                            }

                            var rightProperty = CurrentToken?.Token;
                            if (rightProperty == "_")
                            {
                                rightProperty = property;
                            }
                            
                            NextToken();

                            return ParseResult.Success(new PropertyComparisonExpression(leftDataPart, property, filterOperator, rightDataPart, rightProperty));
                        }
                        else
                        {
                            // This is a comparison with a value
                            return ParseResult.Success(new PropertyComparisonExpression(leftDataPart, property, filterOperator, null, rightIdentifier));
                        }
                    }
                    else if (CurrentToken?.Type == FilterTokenType.String || CurrentToken?.Type == FilterTokenType.Number 
                                                                          || CurrentToken?.Type == FilterTokenType.DateTime)
                    {
                        var value = CurrentToken?.Token;
                        NextToken();
                        return ParseResult.Success(new PropertyComparisonExpression(leftDataPart, property, filterOperator, null, value));
                    }
                    else
                    {
                        return ParseResult.Incomplete("Expected value after operator");
                    }
                }
                catch (ArgumentException ex)
                {
                    return ParseResult.Incomplete(ex.Message);
                }
            }
            else
            {
                // Simple text search
                return ParseResult.Success(new TextSearchExpression(identifier));
            }
        }

        if (CurrentToken?.Type == FilterTokenType.Colon && CurrentToken?.Token == ":")
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Identifier)
            {
                return ParseResult.Incomplete("Expected identifier after colon");
            }
        
            var identifier = CurrentToken?.Token.ToLowerInvariant();
            NextToken();
            
            return ParseResult.Incomplete($"Unknown filter type: {identifier}");
        }

        // Simple text search as fallback
        var textSearchExpression = new TextSearchExpression(CurrentToken?.Token);
        NextToken();
        return ParseResult.Success(textSearchExpression);
    }

    private void NextToken()
    {
        CurrentToken = _tokenizer.GetNextToken();
    }
}