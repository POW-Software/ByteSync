using ByteSync.Business.Filtering.Expressions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Business.Filtering.Parsing;

public class FilterParser : IFilterParser
{
    private readonly IDataPartIndexer _dataPartIndexer;
    private readonly IOperatorParser _operatorParser;
    
    private string _filterText;
    private int _position;
    private string _currentToken;
    private FilterTokenType _currentTokenType;

    private enum FilterTokenType
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

    public FilterParser(IDataPartIndexer dataPartIndexer, IOperatorParser operatorParser)
    {
        _dataPartIndexer = dataPartIndexer;
        _operatorParser = operatorParser;
    }

    public FilterExpression Parse(string filterText)
    {
        _filterText = filterText ?? string.Empty;
        _position = 0;
        _currentToken = string.Empty;
        NextToken();
        
        if (string.IsNullOrWhiteSpace(_filterText))
            return new TrueExpression();

        // Split by whitespace for simple text search
        var terms = _filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

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

        while (_currentTokenType == FilterTokenType.LogicalOperator &&
               (_currentToken.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                _currentToken.Equals("||", StringComparison.OrdinalIgnoreCase)))
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

        while (_currentTokenType == FilterTokenType.LogicalOperator &&
               (_currentToken.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                _currentToken.Equals("&&", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var right = ParseFactor();
            left = new AndExpression(left, right);
        }

        return left;
    }

    private FilterExpression ParseFactor()
    {
        if (_currentTokenType == FilterTokenType.LogicalOperator &&
            (_currentToken.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
             _currentToken.Equals("!", StringComparison.OrdinalIgnoreCase)))
        {
            NextToken();
            var expression = ParseFactor();
            return new NotExpression(expression);
        }

        if (_currentTokenType == FilterTokenType.OpenParenthesis)
        {
            NextToken();
            var expression = ParseExpression();

            if (_currentTokenType != FilterTokenType.CloseParenthesis)
                throw new InvalidOperationException("Expected closing parenthesis");

            NextToken();
            return expression;
        }

        if (_currentTokenType == FilterTokenType.Identifier && _currentToken.Equals("wb", StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (_currentTokenType != FilterTokenType.Colon)
                throw new InvalidOperationException("Expected colon after 'wb'");

            NextToken();
            var baseExpression = ParseFactor();
            return new FutureStateExpression(baseExpression);
        }

        if (_currentTokenType == FilterTokenType.Identifier && _currentToken.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            NextToken();
            if (_currentTokenType != FilterTokenType.Colon)
                throw new InvalidOperationException("Expected colon after 'on'");

            NextToken();
            if (_currentTokenType != FilterTokenType.Identifier)
                throw new InvalidOperationException("Expected data source identifier after 'on:'");

            var dataSource = _currentToken;
            NextToken();
            return new ExistsExpression(dataSource);
        }

        if (_currentTokenType == FilterTokenType.Identifier)
        {
            var identifier = _currentToken;
            NextToken();

            if (_currentTokenType == FilterTokenType.Dot)
            {
                NextToken();
                if (_currentTokenType != FilterTokenType.Identifier)
                {
                    throw new InvalidOperationException("Expected property name after dot");
                }

                var property = _currentToken;
                NextToken();

                if (_currentTokenType != FilterTokenType.Operator)
                {
                    throw new InvalidOperationException("Expected operator after property name");
                }

                var op = _currentToken;
                NextToken();
                
                var filterOperator = _operatorParser.Parse(op);
                
                var leftDataPart = _dataPartIndexer.GetDataPart(identifier)!;

                // Check if the right side is a data source or a value
                if (_currentTokenType == FilterTokenType.Identifier)
                {
                    var rightIdentifier = _currentToken;
                    NextToken();

                    if (_currentTokenType == FilterTokenType.Dot)
                    {
                        // This is a comparison between two properties
                        NextToken();
                        if (_currentTokenType != FilterTokenType.Identifier)
                            throw new InvalidOperationException("Expected property name after dot");
                        
                        var rightDataPart = _dataPartIndexer.GetDataPart(rightIdentifier)!;

                        var rightProperty = _currentToken;
                        NextToken();

                        return new PropertyComparisonExpression(leftDataPart, property, filterOperator, rightDataPart, rightProperty);
                    }
                    else
                    {
                        // This is a comparison with a value
                        return new PropertyComparisonExpression(leftDataPart, property, filterOperator, null, rightIdentifier);
                    }
                }
                else if (_currentTokenType == FilterTokenType.String || _currentTokenType == FilterTokenType.Number)
                {
                    var value = _currentToken;
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

        if (_currentTokenType == FilterTokenType.Colon && _currentToken == ":")
        {
            NextToken();
            if (_currentTokenType != FilterTokenType.Identifier)
                throw new InvalidOperationException("Expected identifier after colon");

            var identifier = _currentToken.ToLowerInvariant();
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
        var textSearchExpression = new TextSearchExpression(_currentToken);
        NextToken();
        return textSearchExpression;
    }

    private void NextToken()
    {
        // Skip whitespace
        while (_position < _filterText.Length && char.IsWhiteSpace(_filterText[_position]))
            _position++;

        if (_position >= _filterText.Length)
        {
            _currentToken = string.Empty;
            _currentTokenType = FilterTokenType.End;
            return;
        }

        var c = _filterText[_position];

        if (c == '(')
        {
            _currentToken = "(";
            _currentTokenType = FilterTokenType.OpenParenthesis;
            _position++;
        }
        else if (c == ')')
        {
            _currentToken = ")";
            _currentTokenType = FilterTokenType.CloseParenthesis;
            _position++;
        }
        else if (c == '.')
        {
            _currentToken = ".";
            _currentTokenType = FilterTokenType.Dot;
            _position++;
        }
        else if (c == ':')
        {
            _currentToken = ":";
            _currentTokenType = FilterTokenType.Colon;
            _position++;
        }
        else if (c == '"' || c == '\'')
        {
            var quoteChar = c;
            _position++;
            var start = _position;

            while (_position < _filterText.Length && _filterText[_position] != quoteChar)
                _position++;

            if (_position < _filterText.Length)
            {
                _currentToken = _filterText.Substring(start, _position - start);
                _currentTokenType = FilterTokenType.String;
                _position++;
            }
            else
            {
                _currentToken = _filterText.Substring(start);
                _currentTokenType = FilterTokenType.String;
            }
        }
        else if (char.IsDigit(c))
        {
            var start = _position;

            while (_position < _filterText.Length &&
                   (char.IsDigit(_filterText[_position]) || _filterText[_position] == '.'))
            {
                _position++;
            }

            _currentToken = _filterText.Substring(start, _position - start);
            _currentTokenType = FilterTokenType.Number;
        }
        else if (c == '=' || c == '!' || c == '<' || c == '>' || c == '~')
        {
            var start = _position;
            _position++;

            if (_position < _filterText.Length &&
                (_filterText[_position] == '=' || _filterText[_position] == '~'))
            {
                _position++;
            }

            _currentToken = _filterText.Substring(start, _position - start);
            _currentTokenType = FilterTokenType.Operator;
        }
        else
        {
            var start = _position;

            while (_position < _filterText.Length &&
                   (char.IsLetterOrDigit(_filterText[_position]) || _filterText[_position] == '_'))
                _position++;

            _currentToken = _filterText.Substring(start, _position - start);

            if (_currentToken.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                _currentToken.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                _currentToken.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
                _currentToken == "&&" || _currentToken == "||")
            {
                _currentTokenType = FilterTokenType.LogicalOperator;
            }
            else
            {
                if (_dataPartIndexer.GetDataPart(_currentToken) != null || _currentToken.ToLower().Equals("content"))
                {
                    _currentTokenType = FilterTokenType.Identifier;
                }
                else
                {
                    _currentTokenType = FilterTokenType.String;
                }
            }
        }
    }
}
    