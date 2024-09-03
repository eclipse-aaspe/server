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

    public string GenerateSql(AstNode node, string typePrefix, ref int upperCountTypePrefix, string filterType = "")
    {
        var leftSql = "ERROR";
        var rightSql = "ERROR";
        string op = "NOT SUPPORTED";
        int countTypePrefix = 0;

        upperCountTypePrefix = 0;
        switch (node)
        {
            case QueryNode queryNode:
                for (int fIndex = 0; fIndex < queryNode.FilterDeclarations.Count; fIndex++)
                {
                    if (queryNode.FilterTypes[fIndex] == filterType)
                    {
                        return GenerateSql(queryNode.FilterDeclarations[fIndex], typePrefix, ref upperCountTypePrefix);
                    }
                }
                return "";
            case FilterDeclarationNode filterDeclarationNode:
                var filter = GenerateSql(filterDeclarationNode.FilterExpression, typePrefix, ref countTypePrefix);
                upperCountTypePrefix += countTypePrefix;
                if (filter == "$SKIP")
                {
                    filter = "";
                }
                while (filter.Contains("(true && true)") || filter.Contains("(true || true)") || filter.Contains("(true)"))
                {
                    filter = filter.Replace("(true && true)", "true");
                    filter = filter.Replace("(true || true)", "true");
                    filter = filter.Replace("(true)", "true");
                }
                return filter;
            case SingleComparisonNode singleComparisonNode:
                bool isNum = false;
                bool isStr = false;
                bool withDot = false;
                leftSql = GenerateSql(singleComparisonNode.Left, typePrefix, ref countTypePrefix);
                upperCountTypePrefix += countTypePrefix;
                rightSql = GenerateSql(singleComparisonNode.Right, typePrefix, ref countTypePrefix);
                upperCountTypePrefix += countTypePrefix;
                if (leftSql == "$SKIP" || rightSql == "$SKIP")
                {
                    return "$SKIP";
                }
                switch (singleComparisonNode.ComparisonType)
                {
                    case TokenType.StrEq:
                        op = "==";
                        isStr = true;
                        break;
                    case TokenType.StrNe:
                        op = "!=";
                        isStr = true;
                        break;
                    case TokenType.StrGt:
                        op = ">";
                        isStr = true;
                        break;
                    case TokenType.StrLt:
                        op = "<";
                        isStr = true;
                        break;
                    case TokenType.StrGe:
                        op = ">=";
                        isStr = true;
                        break;
                    case TokenType.StrLe:
                        op = "<=";
                        isStr = true;
                        break;
                    case TokenType.StrStarts:
                        op = "StartsWith";
                        isStr = true;
                        withDot = true;
                        break;
                    case TokenType.StrEnds:
                        op = "EndsWith";
                        isStr = true;
                        withDot = true;
                        break;
                    case TokenType.StrContains:
                        op = "Contains";
                        isStr = true;
                        withDot = true;
                        break;
                    case TokenType.NumEq:
                        op = "==";
                        isNum = true;
                        break;
                    case TokenType.NumNe:
                        op = "!=";
                        isNum = true;
                        break;
                    case TokenType.NumGt:
                        op = ">";
                        isNum = true;
                        break;
                    case TokenType.NumLt:
                        op = "<";
                        isNum = true;
                        break;
                    case TokenType.NumGe:
                        op = ">=";
                        isNum = true;
                        break;
                    case TokenType.NumLe:
                        op = "<=";
                        isNum = true;
                        break;
                }
                if (typePrefix != "sme.value")
                {
                    if (leftSql.ToLower() == "sme.value")
                    {
                        if (isStr)
                        {
                            leftSql = "sValue";
                        }
                        if (isNum)
                        {
                            leftSql = "mValue";
                        }
                        if (typePrefix != "" && typePrefix != leftSql)
                        {
                            return "$SKIPVALUE";
                        }
                        // for sme.value increase here
                        upperCountTypePrefix++;
                    }
                    if (rightSql.ToLower() == "sme.value")
                    {
                        if (isStr)
                        {
                            rightSql = "sValue";
                        }
                        if (isNum)
                        {
                            rightSql = "mValue";
                        }
                        if (typePrefix != "" && typePrefix != rightSql)
                        {
                            return "$SKIPVALUE";
                        }
                        // for sme.value increase here
                        upperCountTypePrefix++;
                    }
                }
                if (withDot)
                {
                    return $"{leftSql}.{op}({rightSql})";
                }
                return $"({leftSql} {op} {rightSql})";
            case LogicalOperatorNode logicalOperatorNode:
                bool first = true;
                string result = "";
                int countSkipValue = 0;

                countTypePrefix = 0;

                for (int i = 0; i < logicalOperatorNode.Operands.Count; i++)
                {
                    var s = GenerateSql(logicalOperatorNode.Operands[i], typePrefix, ref countTypePrefix);
                    upperCountTypePrefix += countTypePrefix;
                    if (s == "$SKIPVALUE")
                    {
                        countSkipValue++;
                    }
                    else
                    {
                        if (s != "$SKIP")
                        {
                            if (first)
                            {
                                first = false;
                                result = s;
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
                            }
                            else
                            {
                                result += " " + op + " " + s;
                            }
                        }
                    }
                }
                if (result == "")
                {
                    if (countTypePrefix == 0 && countSkipValue == 0)
                    {
                        return "true";
                    }
                    return "$SKIP";
                }
                return "(" + result + ")";
            case IdentifierNode identifierNode:
                if (typePrefix == "sValue" || typePrefix == "mValue")
                {
                    if (identifierNode.Name != "sme.value")
                    {
                        return "$SKIP";
                    }
                    // upperCountTypePrefix++ at str_ or num_ expression
                    return "sme.value";
                }
                if (typePrefix == "sme." && identifierNode.Name == "sme.value")
                    return "$SKIP";
                if (typePrefix != "" && !identifierNode.Name.StartsWith(typePrefix))
                    return "$SKIP";
                upperCountTypePrefix++;
                return identifierNode.Name;
            case StringLiteralNode stringLiteralNode:
                return stringLiteralNode.Value;
            case NumericalLiteralNode numericalLiteralNode:
                return numericalLiteralNode.Value;
            default:
                throw new NotSupportedException("Unknown node type");
        }
    }
}

