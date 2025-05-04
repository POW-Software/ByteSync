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

    public FilterExpression Parse(string filterText)
    {
        _tokenizer.Initialize(filterText ?? string.Empty);
        CurrentToken = null;
        NextToken();
        
        if (string.IsNullOrWhiteSpace(filterText))
            return new TrueExpression();

        // Split by whitespace for simple text search
        var terms = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Check if there are any special expressions
        if (!terms.Any(t => t.Contains(":") || t.Contains(".") || t.Contains("(") ||
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

            return compositeExpression;
        }

        // Otherwise, parse the expression
        return ParseExpression();
    }

    private FilterExpression ParseExpression()
    {
        var left = ParseTerm();

        while (CurrentToken?.Type == FilterTokenType.LogicalOperator &&
               (CurrentToken.Token.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                CurrentToken.Token.Equals("||", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var right = ParseTerm();
            left = new OrExpression(left, right);
        }

        return left;
    }

    private FilterExpression ParseTerm()
    {
        var left = ParseFactor();

        while (CurrentToken?.Type == FilterTokenType.LogicalOperator &&
               (CurrentToken.Token.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                CurrentToken.Token.Equals("&&", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var right = ParseFactor();
            left = new AndExpression(left, right);
        }

        return left;
    }

    private FilterExpression ParseFactor()
    {
        if (CurrentToken?.Type == FilterTokenType.LogicalOperator &&
            (CurrentToken.Token.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
             CurrentToken.Token.Equals("!", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var expression = ParseFactor();
            return new NotExpression(expression);
        }

        if (CurrentToken?.Type == FilterTokenType.OpenParenthesis)
        {
            NextToken();
            var expression = ParseExpression();

            if (CurrentToken?.Type != FilterTokenType.CloseParenthesis)
                throw new InvalidOperationException("Expected closing parenthesis");

            NextToken();
            return expression;
        }

        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals("wb", StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Colon)
                throw new InvalidOperationException("Expected colon after 'wb'");

            NextToken();
            var baseExpression = ParseFactor();
            return new FutureStateExpression(baseExpression);
        }

        if (CurrentToken?.Type == FilterTokenType.Identifier && CurrentToken.Token.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Colon)
                throw new InvalidOperationException("Expected colon after 'on'");

            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Identifier)
                throw new InvalidOperationException("Expected data source identifier after 'on:'");

            var dataSource = CurrentToken?.Token;
            NextToken();
            return new ExistsExpression(dataSource);
        }

        if (CurrentToken?.Type == FilterTokenType.Identifier)
        {
            var identifier = CurrentToken?.Token;
            NextToken();

            if (CurrentToken?.Type == FilterTokenType.Dot)
            {
                NextToken();
                if (CurrentToken?.Type != FilterTokenType.Identifier)
                {
                    throw new InvalidOperationException("Expected property name after dot");
                }

                var property = CurrentToken?.Token;
                NextToken();

                if (CurrentToken?.Type != FilterTokenType.Operator)
                {
                    throw new InvalidOperationException("Expected operator after property name");
                }

                var op = CurrentToken?.Token;
                NextToken();
                
                var filterOperator = _operatorParser.Parse(op);
                
                var leftDataPart = _dataPartIndexer.GetDataPart(identifier)!;

                // Check if the right side is a data source or a value
                if (CurrentToken?.Type == FilterTokenType.Identifier)
                {
                    var rightIdentifier = CurrentToken?.Token;
                    NextToken();

                    if (CurrentToken?.Type == FilterTokenType.Dot)
                    {
                        // This is a comparison between two properties
                        NextToken();
                        if (CurrentToken?.Type != FilterTokenType.Identifier)
                            throw new InvalidOperationException("Expected property name after dot");
                        
                        var rightDataPart = _dataPartIndexer.GetDataPart(rightIdentifier)!;

                        var rightProperty = CurrentToken?.Token;
                        NextToken();

                        return new PropertyComparisonExpression(leftDataPart, property, filterOperator, rightDataPart, rightProperty);
                    }
                    else
                    {
                        // This is a comparison with a value
                        return new PropertyComparisonExpression(leftDataPart, property, filterOperator, null, rightIdentifier);
                    }
                }
                else if (CurrentToken?.Type == FilterTokenType.String || CurrentToken?.Type == FilterTokenType.Number)
                {
                    var value = CurrentToken?.Token;
                    NextToken();
                    return new PropertyComparisonExpression(leftDataPart, property, filterOperator, null, value);
                }
                else
                {
                    throw new InvalidOperationException("Expected value after operator");
                }
            }
            else
            {
                // Simple text search
                return new TextSearchExpression(identifier);
            }
        }

        if (CurrentToken?.Type == FilterTokenType.Colon && CurrentToken?.Token == ":")
        {
            NextToken();
            if (CurrentToken?.Type != FilterTokenType.Identifier)
                throw new InvalidOperationException("Expected identifier after colon");

            var identifier = CurrentToken?.Token.ToLowerInvariant();
            NextToken();

            if (identifier == "file")
            {
                return new FileSystemTypeExpression(FileSystemTypes.File);
            }
            else if (identifier == "dir" || identifier == "directory")
            {
                return new FileSystemTypeExpression(FileSystemTypes.Directory);
            }
            else if (identifier.StartsWith("only"))
            {
                var letter = identifier.Substring(4).ToUpperInvariant();
                return new OnlyExpression(letter);
            }
            else if (identifier.StartsWith("ison"))
            {
                var letter = identifier.Substring(4).ToUpperInvariant();
                return new ExistsExpression(letter);
            }
            else
            {
                throw new InvalidOperationException($"Unknown filter type: {identifier}");
            }
        }

        // Simple text search as fallback
        var textSearchExpression = new TextSearchExpression(CurrentToken?.Token);
        NextToken();
        return textSearchExpression;
    }

    private void NextToken()
    {
        CurrentToken = _tokenizer.GetNextToken();
    }
}