using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Business.Filtering.Parsing;

public class FilterTokenizer : IFilterTokenizer
{
    private readonly IDataPartIndexer _dataPartIndexer;
    private string _filterText;
    private int _position;

    public FilterTokenizer(IDataPartIndexer dataPartIndexer)
    {
        _dataPartIndexer = dataPartIndexer;
    }

    public void Initialize(string filterText)
    {
        _filterText = filterText ?? string.Empty;
        _position = 0;
    }

    public FilterToken GetNextToken()
    {
        // Skip whitespace
        while (_position < _filterText.Length && char.IsWhiteSpace(_filterText[_position]))
            _position++;

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
                _position++;

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
            var start = _position;

            while (_position < _filterText.Length &&
                   (char.IsDigit(_filterText[_position]) || _filterText[_position] == '.'))
            {
                _position++;
            }
            
            while (_position < _filterText.Length && char.IsLetter(_filterText[_position]))
            {
                _position++;
            }
            
            var tokenValue = _filterText.Substring(start, _position - start);

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
                   (char.IsLetterOrDigit(_filterText[_position]) || _filterText[_position] == '_'))
                _position++;

            var currentToken = _filterText.Substring(start, _position - start);
            FilterTokenType currentTokenType;

            if (currentToken.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                currentToken.Equals("OR", StringComparison.OrdinalIgnoreCase) ||
                currentToken.Equals("NOT", StringComparison.OrdinalIgnoreCase) ||
                currentToken == "&&" || currentToken == "||")
            {
                currentTokenType = FilterTokenType.LogicalOperator;
            }
            else
            {
                if (_dataPartIndexer.GetDataPart(currentToken) != null || currentToken == "B1" ||
                    Enum.GetNames(typeof(PropertyType)).Any(name => currentToken.ToLower().Equals(name.ToLower())) ||
                    Enum.GetNames(typeof(SpecialOperator)).Any(name => currentToken.ToLower().Equals(name.ToLower())))
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
}