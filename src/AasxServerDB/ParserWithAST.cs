using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QueryParserTest;
public class ParserWithAST
{
    private readonly Lexer _lexer;
    private Token _currentToken;

    public ParserWithAST(Lexer lexer)
    {
        _lexer = lexer;
        _currentToken = _lexer.GetNextToken();
    }

    private void Eat(TokenType tokenType)
    {
        if (_currentToken.Type == tokenType)
        {
            _currentToken = _lexer.GetNextToken();
        }
        else
        {
            int position = _lexer.GetPosition();
            throw new Exception($"Unexpected token: {_currentToken.Type}  {_currentToken.Value} at position {position}, expected token: {tokenType}");
        }
    }

    public QueryNode Parse()
    {
        return Query();
    }

    // <query> ::= ( <queryParameter> <whitespace> )* <queryParameter> <whitespace>*
    private QueryNode Query()
    {
        var queryNode = new QueryNode();
        while (_currentToken.Type == TokenType.Filter || _currentToken.Type == TokenType.Whitespace)
        {
            if (_currentToken.Type == TokenType.Filter)
            {
                queryNode.FilterTypes.Add(_currentToken.Value);
            }
            var filterDeclaration = QueryParameter();
            queryNode.FilterDeclarations.Add(filterDeclaration);
            while (_currentToken.Type == TokenType.Whitespace)
            {
                Eat(TokenType.Whitespace);
            }
        }
        return queryNode;
    }

    //<queryParameter> ::= <filterDeclaration>
    private FilterDeclarationNode QueryParameter()
    {
        return FilterDeclaration();
    }

    //<filterDeclaration> ::= "filter" "=" <filterExpression>
    private FilterDeclarationNode FilterDeclaration()
    {
        Eat(TokenType.Filter);
        Eat(TokenType.Equals);
        var filterExpression = FilterExpression();
        return new FilterDeclarationNode(filterExpression);
    }

    //<filterExpression> ::= <singleComparison> | <logicalOperator>
    private FilterExpressionNode FilterExpression()
    {
        if (_currentToken.Type.ToString().StartsWith("Str") || _currentToken.Type.ToString().StartsWith("Num") || _currentToken.Type.ToString().StartsWith("Dt"))
        {
            return SingleComparison();
        }
        else if (_currentToken.Type == TokenType.Not || _currentToken.Type == TokenType.And || _currentToken.Type == TokenType.Or)
        {
            return LogicalOperator();
        }
        else
        {
            int position = _lexer.GetPosition();
            throw new Exception($"Unexpected token in filter expression: {_currentToken.Type} {_currentToken.Value} at position {position}");
        }
    }

    //<singleComparison> ::=
    // (
    // "str_" ( "eq" | "ne(" | "gt"  | "lt" | "ge" | "le" | "starts" | "ends" | "contains" )
    // "(" ( <identifer> | <StringLiteral> ) "," ( <StringLiteral> | <identifer> ) ")"
    // )
    // |
    // (
    // "num_" ( "eq" | "ne(" | "gt"  | "lt" | "ge" | "le" )
    // "(" ( <identifer> | <NumericalLiteral> ) "," ( <NumericalLiteral> | <identifer> ) ")"
    // )
    // |
    // (
    // "dt_" ( "eq" | "ne(" | "gt"  | "lt" | "ge" | "le" )
    // "(" ( <identifer> | <StringLiteral> ) "," ( <StringLiteral> | <identifer> ) ")"
    // )
    private SingleComparisonNode SingleComparison()
    {
        var comparisonType = _currentToken.Type;
        Eat(comparisonType);
        Eat(TokenType.OpenParen);
        var left = ParseLiteralOrIdentifier();
        Eat(TokenType.Comma);
        var right = ParseLiteralOrIdentifier();
        Eat(TokenType.CloseParen);
        return new SingleComparisonNode(comparisonType, left, right);
    }

    //<logicalOperator> ::= ( "not(" <filterExpression> ")" ) | ( "and" <filterList> ) | ( "or" <filterList> )
    private FilterExpressionNode LogicalOperator()
    {
        var operatorType = _currentToken.Type;
        Eat(operatorType);
        var operands = FilterList();
        return new LogicalOperatorNode(operatorType, operands);
    }

    //<filterList> ::= "(" <filterExpression> ( "," <filterExpression> )* ")"
    private List<FilterExpressionNode> FilterList()
    {
        var filterExpressions = new List<FilterExpressionNode>();
        Eat(TokenType.OpenParen);
        filterExpressions.Add(FilterExpression());
        while (_currentToken.Type == TokenType.Comma)
        {
            Eat(TokenType.Comma);
            filterExpressions.Add(FilterExpression());
        }
        Eat(TokenType.CloseParen);
        return filterExpressions;
    }

    private AstNode ParseLiteralOrIdentifier()
    {
        if (_currentToken.Type == TokenType.Identifier)
        {
            var identifier = new IdentifierNode(_currentToken.Value);
            Eat(TokenType.Identifier);
            return identifier;
        }
        else if (_currentToken.Type == TokenType.StringLiteral)
        {
            var stringLiteral = new StringLiteralNode(_currentToken.Value);
            Eat(TokenType.StringLiteral);
            return stringLiteral;
        }
        else if (_currentToken.Type == TokenType.NumericalLiteral)
        {
            var numericalLiteral = new NumericalLiteralNode(_currentToken.Value);
            Eat(TokenType.NumericalLiteral);
            return numericalLiteral;
        }
        else
        {
            int position = _lexer.GetPosition();
            throw new Exception($"Unexpected token in literal or identifier: {_currentToken.Type} {_currentToken.Value} at position {position}");
        }
    }

    public string GenerateSql(AstNode node, string typePrefix, string filterType = "")
    {
        var leftSql = "ERROR";
        var rightSql = "ERROR";
        string op = "NOT SUPPORTED";
        switch (node)
        {
            case QueryNode queryNode:
                for (int fIndex = 0; fIndex < queryNode.FilterDeclarations.Count; fIndex++)
                {
                    if (queryNode.FilterTypes[fIndex] == filterType)
                    {
                        return GenerateSql(queryNode.FilterDeclarations[fIndex], typePrefix);
                    }
                }
                return "";
            case FilterDeclarationNode filterDeclarationNode:
                return GenerateSql(filterDeclarationNode.FilterExpression, typePrefix);
            case SingleComparisonNode singleComparisonNode:
                leftSql = GenerateSql(singleComparisonNode.Left, typePrefix);
                rightSql = GenerateSql(singleComparisonNode.Right, typePrefix);
                switch (singleComparisonNode.ComparisonType)
                {
                    case TokenType.StrEq:
                        op = "==";
                        break;
                    case TokenType.StrNe:
                        op = "!=";
                        break;
                    case TokenType.StrGt:
                        op = ">";
                        break;
                    case TokenType.StrLt:
                        op = "<";
                        break;
                    case TokenType.StrGe:
                        op = ">=";
                        break;
                    case TokenType.StrLe:
                        op = "<=";
                        break;
                    case TokenType.StrStarts:
                        op = "StartsWith";
                        return $"{leftSql}.{op}({rightSql})";
                    case TokenType.StrEnds:
                        op = "EndsWith";
                        return $"{leftSql}.{op}({rightSql})";
                    case TokenType.StrContains:
                        op = "Contains";
                        return $"{leftSql}.{op}({rightSql})";
                    case TokenType.NumEq:
                        op = "==";
                        break;
                    case TokenType.NumNe:
                        op = "!=";
                        break;
                    case TokenType.NumGt:
                        op = ">";
                        break;
                    case TokenType.NumLt:
                        op = "<";
                        break;
                    case TokenType.NumGe:
                        op = ">=";
                        break;
                    case TokenType.NumLe:
                        op = "<=";
                        break;
                }
                return $"({leftSql} {op} {rightSql})";
            case LogicalOperatorNode logicalOperatorNode:
                string result = GenerateSql(logicalOperatorNode.Operands[0], typePrefix);
                switch (logicalOperatorNode.OperatorType)
                {
                    case TokenType.Not:
                        return $"!{result}";
                    case TokenType.And:
                        op = "&&";
                        break;
                    case TokenType.Or:
                        op = "||";
                        break;
                }
                if (logicalOperatorNode.Operands.Count > 1)
                {
                    for (int i = 1; i < logicalOperatorNode.Operands.Count; i++)
                    {
                        result += " " + op + " " + GenerateSql(logicalOperatorNode.Operands[i], typePrefix);
                    }
                    result = "(" + result + ")";
                }
                return result ;
            case IdentifierNode identifierNode:
                return identifierNode.Name;
                break ;
            case StringLiteralNode stringLiteralNode:
                return stringLiteralNode.Value;
                break ;
            case NumericalLiteralNode numericalLiteralNode:
                return numericalLiteralNode.Value;
                break ;
            default:
                throw new NotSupportedException("Unknown node type");
        }
    }
}

