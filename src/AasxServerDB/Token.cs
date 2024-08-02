
namespace QueryParserTest;

public enum TokenType
{
    Filter, StrEq, StrNe, StrGt, StrLt, StrGe, StrLe, StrStarts, StrEnds, StrContains, NumEq, NumNe, NumGt, NumLt, NumGe, NumLe, DtEq, DtNe, DtGt, DtLt, DtGe, DtLe, Not, And, Or, Identifier, StringLiteral, NumericalLiteral, OpenParen, CloseParen, Comma, Equals, Whitespace, EOF
}
public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }
}
