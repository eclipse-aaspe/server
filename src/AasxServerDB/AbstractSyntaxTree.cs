using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryParserTest;

public abstract class AstNode
{
    public abstract override string ToString();
}

public class QueryNode : AstNode
{
    public List<string> FilterTypes = new List<string>();
    public List<FilterDeclarationNode> FilterDeclarations { get; } = new List<FilterDeclarationNode>();

    public override string ToString()
    {
        return $"Query: [{string.Join(", ", FilterDeclarations)}]";
    }
}

public class FilterDeclarationNode : AstNode
{
    public FilterExpressionNode FilterExpression { get; }

    public FilterDeclarationNode(FilterExpressionNode filterExpression)
    {
        FilterExpression = filterExpression;
    }

    public override string ToString()
    {
        return $"FilterDeclaration: {FilterExpression}";
    }
}

public abstract class FilterExpressionNode : AstNode { }

public class SingleComparisonNode : FilterExpressionNode
{
    public TokenType ComparisonType { get; }
    public AstNode Left { get; }
    public AstNode Right { get; }

    public SingleComparisonNode(TokenType comparisonType, AstNode left, AstNode right)
    {
        ComparisonType = comparisonType;
        Left = left;
        Right = right;
    }

    public override string ToString()
    {
        return $"SingleComparison: {ComparisonType}({Left}, {Right})";
    }
}

public class LogicalOperatorNode : FilterExpressionNode
{
    public TokenType OperatorType { get; }
    public List<FilterExpressionNode> Operands { get; }

    public LogicalOperatorNode(TokenType operatorType, List<FilterExpressionNode> operands)
    {
        OperatorType = operatorType;
        Operands = operands;
    }

    public override string ToString()
    {
        return $"LogicalOperator: {OperatorType}({string.Join(", ", Operands)})";
    }
}

public class IdentifierNode : AstNode
{
    public string Name { get; }

    public IdentifierNode(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return $"Identifier: {Name}";
    }
}

public class StringLiteralNode : AstNode
{
    public string Value { get; }

    public StringLiteralNode(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"StringLiteral: \"{Value}\"";
    }
}

public class NumericalLiteralNode : AstNode
{
    public string Value { get; }

    public NumericalLiteralNode(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"NumericalLiteral: {Value}";
    }
}