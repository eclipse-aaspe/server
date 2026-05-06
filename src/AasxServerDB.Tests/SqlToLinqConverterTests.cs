namespace AasxServerDB.Tests;

using Contracts;
using FluentAssertions;

public sealed class SqlToLinqConverterTests
{
    [Fact]
    public void Convert_TopLevelOrBetweenParenthesizedGroups_ParsesWholeExpression()
    {
        const string sql = "(((\"IdShort\" = 'Nameplate') OR (\"IdShort\" = 'TechnicalData'))) OR ((\"IdShort\" = 'HandoverDocumentation'))";

        var result = SqlToLinqConverter.Convert(sql, "sm.");

        result.Should().Be("(((idShort == \"Nameplate\") || (idShort == \"TechnicalData\")) || (idShort == \"HandoverDocumentation\"))");
    }
}
