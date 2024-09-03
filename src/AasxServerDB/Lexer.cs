
namespace QueryParserTest;

/*
 <identifer> ::= ( [A-Z] | [a-z] | [1-9] | "." )+
 <whitespace> ::= ( " " | "\n" )+
 <NumericalLiteral> ::= ([0-9] | "." | "E" | ":" )+
 <StringLiteral> ::= "\"" ( [A-Z] | [a-z] | [1-9] | "/" | "*" | "[" | "]" | " " | "@" | "\\" | "+" | "." | ":" | "$" | "^" )+ "\""
*/

public class Lexer
{
    private readonly string _input;
    private int _position;

    private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
    {
        { "filter", TokenType.Filter },
        { "filter_submodel", TokenType.Filter }, { "filter_submodel_elements", TokenType.Filter },
        { "filter_str", TokenType.Filter }, { "filter_num", TokenType.Filter },
        { "str_eq", TokenType.StrEq }, { "str_ne", TokenType.StrNe }, { "str_gt", TokenType.StrGt },
        { "str_lt", TokenType.StrLt }, { "str_ge", TokenType.StrGe }, { "str_le", TokenType.StrLe },
        { "str_starts", TokenType.StrStarts }, { "str_ends", TokenType.StrEnds }, { "str_contains", TokenType.StrContains },
        { "num_eq", TokenType.NumEq }, { "num_ne", TokenType.NumNe }, { "num_gt", TokenType.NumGt },
        { "num_lt", TokenType.NumLt }, { "num_ge", TokenType.NumGe }, { "num_le", TokenType.NumLe },
        { "dt_eq", TokenType.DtEq }, { "dt_ne", TokenType.DtNe }, { "dt_gt", TokenType.DtGt },
        { "dt_lt", TokenType.DtLt }, { "dt_ge", TokenType.DtGe }, { "dt_le", TokenType.DtLe },
        { "not", TokenType.Not }, { "and", TokenType.And }, { "or", TokenType.Or }
    };
    public Lexer(string input) { _input = input; }
    public int GetPosition()
    {
        return _position;
    }
    public Token GetNextToken()
    {
        if (_position >= _input.Length)
            return new Token(TokenType.EOF, string.Empty);

        char currentChar = _input[_position];

        if (char.IsWhiteSpace(currentChar))
        {
            _position++; return new Token(TokenType.Whitespace, " ");
        }
        if (currentChar == '(')
        {
            _position++; return new Token(TokenType.OpenParen, "(");
        }
        if (currentChar == ')')
        {
            _position++; return new Token(TokenType.CloseParen, ")");
        }
        if (currentChar == ',')
        {
            _position++; return new Token(TokenType.Comma, ",");
        }
        if (currentChar == '=')
        {
            _position++; return new Token(TokenType.Equals, "=");
        }
        if (currentChar == '"')
        {
            string strLiteral = ReadStringLiteral();
            return new Token(TokenType.StringLiteral, strLiteral);
        }

        if (char.IsDigit(currentChar) || currentChar == '.' || currentChar == 'E' || currentChar == ':')
        {
            string numLiteral = ReadNumericalLiteral();
            return new Token(TokenType.NumericalLiteral, numLiteral);
        }

        string identifier = ReadIdentifier();
        if (Keywords.ContainsKey(identifier))
        {
            return new Token(Keywords[identifier], identifier);
        }

        return new Token(TokenType.Identifier, identifier);
    }

    private string ReadStringLiteral()
    {
        int start = _position;
        _position++; // Skip the opening quote
        while (_position < _input.Length && _input[_position] != '"')
        {
            _position++;
        }
        _position++; // Skip the closing quote
        return _input.Substring(start, _position - start);
    }

    private string ReadNumericalLiteral()
    {
        int start = _position;
        while (_position < _input.Length && (char.IsDigit(_input[_position]) || _input[_position] == '.' || _input[_position] == 'E' || _input[_position] == ':'))
        {
            _position++;
        }
        return _input.Substring(start, _position - start);
    }

    private string ReadIdentifier()
    {
        int start = _position;
        while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_' || _input[_position] == '.'))
        {
            _position++;
        }
        return _input.Substring(start, _position - start);
    }
}
