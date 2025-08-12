using ByteSync.Interfaces.Services.Filtering;

namespace ByteSync.Business.Filtering.Parsing;

public class FilterTokenizer : IFilterTokenizer
{
    private string _filterText = null!;
    private int _position;

    public void Initialize(string? filterText)
    {
        _filterText = filterText ?? string.Empty;
        _position = 0;
    }

    public FilterToken GetNextToken()
    {
        // Skip whitespace
        while (_position < _filterText.Length && char.IsWhiteSpace(_filterText[_position]))
        {
            _position++;
        }

        if (_position >= _filterText.Length)
        {
            return new FilterToken
            {
                Token = string.Empty,
                Type = FilterTokenType.End
            };
        }

        var c = _filterText[_position];

        if (c == '(')
        {
            _position++;
            return new FilterToken
            {
                Token = "(",
                Type = FilterTokenType.OpenParenthesis
            };
        }
        else if (c == ')')
        {
            _position++;
            return new FilterToken
            {
                Token = ")",
                Type = FilterTokenType.CloseParenthesis
            };
        }
        else if (c == '.')
        {
            _position++;
            return new FilterToken
            {
                Token = ".",
                Type = FilterTokenType.Dot
            };
        }
        else if (c == ':')
        {
            _position++;
            return new FilterToken
            {
                Token = ":",
                Type = FilterTokenType.Colon
            };
        }
        else if (c == '"' || c == '\'')
        {
            var quoteChar = c;
            _position++;
            var start = _position;
            
            while (_position < _filterText.Length && _filterText[_position] != quoteChar)
            {
                _position++;
            }

            string tokenValue;
            if (_position < _filterText.Length)
            {
                tokenValue = _filterText.Substring(start, _position - start);
                _position++;
            }
            else
            {
                tokenValue = _filterText.Substring(start);
            }

            return new FilterToken
            {
                Token = tokenValue,
                Type = FilterTokenType.String
            };
        }
        else if (char.IsDigit(c))
        {
            string tokenValue;
            var start = _position;

            // Parse initial digits
            while (_position < _filterText.Length && char.IsDigit(_filterText[_position]))
            {
                _position++;
            }

            // Check for potential DateTime format
            if (_position < _filterText.Length && _filterText[_position] == '-')
            {
                var dateTimeStart = _position;
                _position++;

                int segmentCount = 1;
                while (_position < _filterText.Length && 
                       (char.IsDigit(_filterText[_position]) || _filterText[_position] == '-'))
                {
                    if (_filterText[_position] == '-')
                    {
                        segmentCount++;
                    }
                    _position++;
                }

                // Validate DateTime format
                if (segmentCount == 2 || segmentCount == 5)
                {
                    tokenValue = _filterText.Substring(start, _position - start);
                    return new FilterToken
                    {
                        Token = tokenValue,
                        Type = FilterTokenType.DateTime
                    };
                }
                else
                {
                    // If not a valid DateTime, fallback to Number
                    _position = dateTimeStart; // Reset position
                }
            }

            // Parse as Number if not DateTime
            while (_position < _filterText.Length &&
                   (char.IsDigit(_filterText[_position]) || _filterText[_position] == '.'))
            {
                _position++;
            }

            while (_position < _filterText.Length && char.IsLetter(_filterText[_position]))
            {
                _position++;
            }

            tokenValue = _filterText.Substring(start, _position - start);

            return new FilterToken
            {
                Token = tokenValue,
                Type = FilterTokenType.Number
            };
        }
        else if (c == '=' || c == '!' || c == '<' || c == '>' || c == '~')
        {
            var start = _position;
            _position++;

            if (_position < _filterText.Length &&
                (_filterText[_position] == '=' || _filterText[_position] == '~' || _filterText[_position] == '>'))
            {
                _position++;
            }

            return new FilterToken
            {
                Token = _filterText.Substring(start, _position - start),
                Type = FilterTokenType.Operator
            };
        }
        else
        {
            var start = _position;

            while (_position < _filterText.Length &&
                   (char.IsLetterOrDigit(_filterText[_position]) 
                    || _filterText[_position] == '-' || _filterText[_position] == '_' 
                    || _filterText[_position] == '\\' || _filterText[_position] == '/'
                    || _filterText[_position] == '*' || _filterText[_position] == '+' || _filterText[_position] == '?'
                    || _filterText[_position] == '^' || _filterText[_position] == '$'
                    || _filterText[_position] == '[' || _filterText[_position] == ']'
                    || _filterText[_position] == '{' || _filterText[_position] == '}'))
            {
                _position++;
            }
            var currentToken = _filterText.Substring(start, _position - start);
            FilterTokenType currentTokenType;

            if (IsLogicalOperator(currentToken))
            {
                currentTokenType = FilterTokenType.LogicalOperator;
            }
            else
            {
                if (IsDataPartOrDataSourceIdentifier(currentToken) ||
                    Identifiers.All().Any(name => currentToken.ToLower().Equals(name.ToLower())))
                {
                    currentTokenType = FilterTokenType.Identifier;
                }
                else
                {
                    currentTokenType = FilterTokenType.String;
                }
            }

            return new FilterToken
            {
                Token = currentToken,
                Type = currentTokenType
            };
        }
    }
    
    private static bool IsLogicalOperator(string token)
    {
        return token.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
               token == "&&" || token == "||";
    }
    
    private static bool IsDataPartOrDataSourceIdentifier(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 3)
        {
            return false;
        }

        if (!char.IsLetter(token[0]))
        {
            return false;
        }

        if (token.Length == 1)
        {
            return true;
        }

        if (token.Length == 2)
        {
            return char.IsLetter(token[1]) || char.IsDigit(token[1]);
        }
        
        return char.IsLetter(token[1]) && char.IsDigit(token[2]);
    }
}