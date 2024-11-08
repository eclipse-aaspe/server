using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AasSecurity.Models;
using Irony.Parsing;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.DependencyInjection;
using QueryParserTest;
using Contracts;

public class QueryGrammar : Grammar
{
    public QueryGrammar(IContractSecurityRules contractSecurityRules) : base(caseSensitive: true)
    {
        mySecurityRules = contractSecurityRules;

        // Define non-terminals
        // <grammar> ::= <queryParameter> | <AllRules>
        // <queryParameter> ::= <logicalExpression>
        // var whitespace = new NonTerminal("whitespace");
        var myGrammar = new NonTerminal("myGrammar");
        var queryParameter = new NonTerminal("queryParameter");
        var allRules = new NonTerminal("AllRules");
        // <logicalExpression> ::= <logicalNestedExpression> | <logicalOrExpression> | <logicalAndExpression> | <logicalNotExpression> | <BoolLiteral> | <castToBool> | <singleComparison>
        // 
        var logicalExpression = new NonTerminal("logicalExpression");
        var logicalNestedExpression = new NonTerminal("logicalNestedExpression");
        var logicalOrExpression = new NonTerminal("logicalOrExpression");
        var logicalAndExpression = new NonTerminal("logicalAndExpression");
        var logicalNotExpression = new NonTerminal("logicalNotExpression");
        var singleComparison = new NonTerminal("singleComparison");
        // <singleComparison> ::= <stringComparison> | <numericalComparison> | <hexComparison> | <boolComparison> | <dateTimeComparison> | <timeComparison>
        // <dateTimeToNum> ::=
        var stringComparison = new NonTerminal("stringComparison");
        var stringOperation = new NonTerminal("stringOperation");
        var numericalComparison = new NonTerminal("numericalComparison");
        var hexComparison = new NonTerminal("hexComparison");
        var boolComparison = new NonTerminal("boolComparison");
        var dateTimeComparison = new NonTerminal("dateTimeComparison");
        var timeComparison = new NonTerminal("timeComparison");
        var dateTimeToNum = new NonTerminal("dateTimeToNum");
        // <operand> ::= <stringOperand> | <numericalOperand> | <hexOperand> | <boolOperand> | <dateTimeOperand> | <timeOperand>
        var operand = new NonTerminal("operand");
        var stringOperand = new NonTerminal("stringOperand");
        var numericalOperand = new NonTerminal("numericalOperand");
        var hexOperand = new NonTerminal("hexOperand");
        var boolOperand = new NonTerminal("boolOperand");
        var dateTimeOperand = new NonTerminal("dateTimeOperand");
        var timeOperand = new NonTerminal("timeOperand");
        // <castToString> <castToNumerical> <castToHex> <castToBool> <castToDateTime> <castToTime>
        var castToString = new NonTerminal("castToString");
        var castToNumerical = new NonTerminal("castToNumerical");
        var castToHex = new NonTerminal("castToHex");
        var castToBool = new NonTerminal("castToBool");
        var castToDateTime = new NonTerminal("castToDateTime");
        var castToTime = new NonTerminal("castToTime");

        // Security
        var defAttributes = new NonTerminal("DefAttributes");
        var defACL = new NonTerminal("DefACL");
        var defObjects = new NonTerminal("DefObjects");
        var defFormula = new NonTerminal("DefFormula");
        var accessRule = new NonTerminal("AccessRule");
        var acl = new NonTerminal("ACL");
        var useAcl = new NonTerminal("UseACL");
        var singleObject = new NonTerminal("SingleObject");
        var objectGroup = new NonTerminal("ObjectGroup");
        var useObjectGroup = new NonTerminal("UseObjectGroup");
        var formula = new NonTerminal("Formula");
        var useFormula = new NonTerminal("UseFormula");
        var right = new NonTerminal("Right");
        var access = new NonTerminal("Access");
        var singleAttribute = new NonTerminal("SingleAttribute");
        var attributeGroup = new NonTerminal("AttributeGroup");
        var useAttributeGroup = new NonTerminal("UseAttributeGroup");
        var claimAttribute = new NonTerminal("ClaimAttribute");
        var globalAttribute = new NonTerminal("GlobalAttribute");
        var referenceAttribute = new NonTerminal("ReferenceAttribute");
        var routeObject = new NonTerminal("RouteObject");
        var identifiableObject = new NonTerminal("IdentifiableObject");
        var referableObject = new NonTerminal("ReferableObject");
        var fragmentObject = new NonTerminal("FragmentObject");
        var descriptorObject = new NonTerminal("DescriptorObject");

        // Terminals
        var stringLiteral = new StringLiteral("StringLiteral", "\"", StringOptions.AllowsAllEscapes);
        var numericalLiteral = new NumberLiteral("NumericalLiteral");
        var hexLiteral = new RegexBasedTerminal("HexLiteral", @"16#[0-9A-F]+");
        var boolLiteral = new RegexBasedTerminal("BoolLiteral", @"true|false");
        var dateTimeLiteral = new RegexBasedTerminal("DateTimeLiteral", @"[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]T[0-9][0-9]:[0-9][0-9]:[0-9][0-9]");
        var timeLiteral = new RegexBasedTerminal("TimeLiteral", @"[0-9][0-9]:[0-9][0-9]:[0-9][0-9]") | new RegexBasedTerminal("TimeLiteral", @"[0-9][0-9]:[0-9][0-9]");
        var FieldIdentifierString = new RegexBasedTerminal("FieldIdentifierString", @"(aas|sm|sme|cd|aasdesc|smdesc)\.(idShort|displayName|description|id|assetKind|assetType|globalAssetId|specificAssetId|submodel|semanticId|value|valueType|submodelDescriptor)");

        var whitespaceTerminal = new RegexBasedTerminal("whitespace", @"[ \n\t\r\f]+");
        NonGrammarTerminals.Add(whitespaceTerminal);

        // lists
        var logicalOrExpressionList = new NonTerminal("logicalOrExpressionList");
        logicalOrExpressionList.Rule = MakeStarRule(logicalOrExpressionList, ToTerm("or") + logicalOrExpression);

        var logicalAndExpressionList = new NonTerminal("logicalAndExpressionList");
        logicalAndExpressionList.Rule = MakeStarRule(logicalAndExpressionList, ToTerm("and") + logicalAndExpression);

        var singleAttributeList = new NonTerminal("singleAttributeList");
        singleAttributeList.Rule = MakeStarRule(singleAttributeList, singleAttribute);

        var rightList = new NonTerminal("rightList");
        rightList.Rule = MakeStarRule(rightList, right);

        var singleObjectList = new NonTerminal("singleObjectList");
        singleObjectList.Rule = MakeStarRule(singleObjectList, singleObject);

        // Define grammar rules
        myGrammar.Rule = queryParameter | allRules;

        queryParameter.Rule = logicalExpression;

        // Query languagae and filter
        logicalExpression.Rule = logicalNestedExpression | logicalOrExpression | logicalAndExpression | logicalNotExpression | boolLiteral | castToBool | singleComparison;

        logicalNestedExpression.Rule = "(" + logicalExpression + ")";

        logicalOrExpression.Rule = logicalExpression + ToTerm("or") + logicalExpression;
        logicalAndExpression.Rule = logicalExpression + ToTerm("and") + logicalExpression;
        logicalNotExpression.Rule = ToTerm("not") + "(" + logicalExpression + ")";

        singleComparison.Rule = stringOperation | stringComparison | numericalComparison | hexComparison | boolComparison | dateTimeComparison | timeComparison;

        var allComparisons = (ToTerm("eq") | "ne" | "gt" | "lt" | "ge" | "le");
        stringOperation.Rule =
            (ToTerm("starts-with") | "ends-with" | "contains" | "regex") +
            "(" + stringOperand + "," + stringOperand + ")";
        stringComparison.Rule =
            stringOperand + allComparisons + stringOperand
            | stringOperand + allComparisons + FieldIdentifierString
            | FieldIdentifierString + allComparisons + stringOperand;
        numericalComparison.Rule = numericalOperand + allComparisons + numericalOperand
            | numericalOperand + allComparisons + FieldIdentifierString
            | FieldIdentifierString + allComparisons + numericalOperand;
        hexComparison.Rule = hexOperand + allComparisons + hexOperand;
        boolComparison.Rule = boolOperand + (ToTerm("eq") | "ne") + boolOperand;
        dateTimeComparison.Rule = dateTimeOperand + allComparisons + dateTimeOperand;
        timeComparison.Rule = timeOperand + allComparisons + timeOperand;
        dateTimeToNum.Rule = (ToTerm("dayOfWeek") | "dayOfMonth" | "month" | "year") + "(" + dateTimeOperand + ")";

        operand.Rule = stringOperand | numericalOperand | hexOperand | boolOperand | dateTimeOperand | timeOperand;
        stringOperand.Rule = FieldIdentifierString | stringLiteral | castToString | singleAttribute;
        numericalOperand.Rule = numericalLiteral | castToNumerical | dateTimeToNum;
        hexOperand.Rule = hexLiteral | castToHex;
        boolOperand.Rule = boolLiteral | castToBool;
        dateTimeOperand.Rule = dateTimeLiteral | castToDateTime | globalAttribute;
        timeOperand.Rule = timeLiteral | castToTime;

        castToString.Rule = ToTerm("str") + "(" + operand + ")";
        castToNumerical.Rule = ToTerm("num") + "(" + operand + ")";
        castToHex.Rule = ToTerm("hex") + "(" + operand + ")";
        castToBool.Rule = ToTerm("bool") + "(" + operand + ")";
        castToDateTime.Rule = ToTerm("dateTime") + "(" + operand + ")";
        castToTime.Rule = ToTerm("time") + "(" + operand + ")";

        // access rules
        allRules.Rule = MakeStarRule(allRules, accessRule);

        accessRule.Rule =
            acl
            + ToTerm("OBJECTS:") + singleObjectList
            + ToTerm("FORMULA:") + formula;

        formula.Rule = logicalExpression;

        acl.Rule = ToTerm("ATTRIBUTES:") + singleAttributeList +
            ToTerm("RIGHTS:") + rightList +
            ToTerm("ACCESS:") + access;

        right.Rule = (ToTerm("CREATE") | "READ" | "UPDATE" | "DELETE" | "EXECUTE" | "VIEW" | "ALL" | "TREE");
        access.Rule = (ToTerm("ALLOW") | "DISABLED");

        singleAttribute.Rule = claimAttribute | globalAttribute | referenceAttribute;
        claimAttribute.Rule = ToTerm("CLAIM") + "(" + stringLiteral + ")";
        globalAttribute.Rule = ToTerm("GLOBAL") + "(" + (ToTerm("LOCALNOW") | "UTCNOW" | "CLIENTNOW" | "ANONYMOUS") + ")";
        referenceAttribute.Rule = ToTerm("REFERENCE") + "(" + stringLiteral + ")";

        singleObject.Rule = routeObject | identifiableObject | referableObject | fragmentObject | descriptorObject;
        routeObject.Rule = ToTerm("ROUTE") + stringLiteral;
        identifiableObject.Rule = ToTerm("IDENTIFIABLE") + stringLiteral;
        referableObject.Rule = ToTerm("REFERABLE") + stringLiteral;
        fragmentObject.Rule = ToTerm("FRAGMENT") + stringLiteral;
        descriptorObject.Rule = ToTerm("DESCRIPTOR") + stringLiteral;

        // Finish up with whitespaces handling
        // This will ensure that we ignore unnecessary whitespaces in parsing
        this.Root = myGrammar;
        // MarkPunctuation("(", ")", ":", ",", "=", "or", "and", "not");
        MarkPunctuation("(", ")", ",", "or", "and", "not");
        // this.RegisterBracePair("(", ")");
        // this.LanguageFlags = LanguageFlags.CreateAst | LanguageFlags.NewLineBeforeEOF;

        // Set whitespace handling
        MarkTransient(myGrammar, queryParameter, logicalExpression, logicalNestedExpression, singleComparison, operand, allRules);
        MarkTransient(stringOperand, numericalOperand, hexOperand, boolOperand, dateTimeOperand, timeOperand);
        MarkTransient(castToString, castToNumerical, castToHex, castToBool, castToDateTime, castToTime);
    }

    private IContractSecurityRules mySecurityRules;

    static List<string> skip = new List<string>() { "str", "num", "hex", "bool" };

    public void PrintParseTree(ParseTreeNode node, int indent, StringWriter sw)
    {
        if (node == null)
            return;

        if (skip.Contains(node.Term.Name))
            return;

        string text = "";
        if ((node.Term.Name.StartsWith("Unnamed") && node.ChildNodes.Count == 1))
        {
            PrintParseTree(node.ChildNodes[0], indent, sw);
        }
        else
        {
            if (!node.Term.Name.StartsWith("Unnamed"))
            {
                text = new string(' ', indent * 2) + node.Term.Name;
            }
            else
            {
                text = new string(' ', indent * 2) + "(" + node.Term.Name + ")";
            }
            if (node.Token != null && node.Token.Value != null)
            {
                if (node.Term.Name != node.Token.Value.ToString())
                {
                    text += " " + node.Token.Value.ToString();
                }
            }
            sw.WriteLine(text);
            foreach (var child in node.ChildNodes)
            {
                PrintParseTree(child, indent + 1, sw);
            }
        }
    }

    public string ParseTreeToExpression(ParseTreeNode node, string typePrefix, ref int upperCountTypePrefix, string parentName = "")
    {
        if (node == null)
            return "";

        if (skip.Contains(node.Term.Name))
            return "";

        else if (node.Term.Name.StartsWith("Unnamed") && node.ChildNodes.Count == 1)
        {
            node = node.ChildNodes[0];
        }

        upperCountTypePrefix = 0;
        var expression = "";
        string op = "";
        string arg1 = "";
        string arg2 = "";
        int countTypePrefix1 = 0;
        int countTypePrefix2 = 0;

        string name = node.Term.Name;
        string value = "";
        if (node.Token != null && node.Token.Value != null)
        {
            value = node.Token.Value.ToString();
        }

        switch (name)
        {
            case "FieldIdentifierString":
                if (value == "sm.id") // patch grammar vs database
                {
                    value = "sm.identifier";
                }
                // complete expression for joined tables
                if (typePrefix == "" && value == "sme.value")
                {
                    switch (parentName)
                    {
                        case "stringOperation":
                        case "stringComparison":
                            return "svalue";
                        case "numericalComparison":
                            return "mvalue";
                    }
                }
                // submodel table
                if (typePrefix == "sm." && value.StartsWith("sm."))
                {
                    upperCountTypePrefix++;
                    return value.Replace("sm.", "");
                }

                // sme table
                if (typePrefix == "sme." && value.StartsWith("sme."))
                {
                    upperCountTypePrefix++;
                    if (value == "sme.value")
                    {
                        return "$SKIP";
                    }
                    return value.Replace("sme.", "");
                }
                // string value table and num value table
                if (typePrefix == "str()" || typePrefix == "num()")
                {
                    upperCountTypePrefix++;
                    if (value != "sme.value")
                    {
                        return "$SKIP";
                    }
                    return "value";
                }

                if (typePrefix != "" && !value.StartsWith(typePrefix))
                {
                    return "$SKIP";
                }
                if (value.StartsWith(typePrefix))
                {
                    upperCountTypePrefix++;
                }
                return value;
            case "StringLiteral":
                if (parentName == "numericalComparison")
                {
                    return value;
                }
                return "\"" + value + "\"";
            case "NumericalLiteral":
                if (parentName == "stringComparison")
                {
                    return "\"" + value + "\"";
                }
                return value;
            case "logicalAndExpression":
            case "logicalOrExpression":
                arg1 = ParseTreeToExpression(node.ChildNodes[0], typePrefix, ref countTypePrefix1);
                arg2 = ParseTreeToExpression(node.ChildNodes[1], typePrefix, ref countTypePrefix2);
                upperCountTypePrefix += countTypePrefix1 + countTypePrefix2;
                if (arg1 == "$SKIP" && arg2 == "$SKIP")
                {
                    if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                    {
                        return "true";
                    }
                    return "$SKIP";
                }
                if (arg1 == "$SKIP" || arg2 == "$SKIP")
                {
                    if (arg1 == "$SKIP" && countTypePrefix1 != 0)
                    {
                        return "true";
                    }
                    if (arg2 == "$SKIP" && countTypePrefix2 != 0)
                    {
                        return "true";
                    }
                    if (arg2 != "$SKIP")
                    {
                        arg1 = arg2;
                    }
                    return arg1;
                }
                if (name == "logicalAndExpression")
                {
                    if (arg1 == "true" && arg2 == "true")
                    {
                        return "true";
                    }
                    if (arg1 == "false" || arg2 == "false")
                    {
                        return "false";
                    }
                    op = "&&";
                }
                else
                {
                    if (arg1 == "true" || arg2 == "true")
                    {
                        return "true";
                    }
                    if (arg1 == "false" && arg2 == "false")
                    {
                        return "false";
                    }
                    op = "||";
                }
                return "(" + arg1 + " " + op + " " + arg2 + ")";
            case "logicalNotExpression":
                arg1 = ParseTreeToExpression(node.ChildNodes[0], typePrefix, ref countTypePrefix1);
                upperCountTypePrefix += countTypePrefix1;
                if (arg1 == "$SKIP")
                {
                    return "$SKIP";
                }
                return "!" + arg1;
            case "stringOperation":
                if (typePrefix == "num()")
                {
                    return "$SKIP";
                }
                op = ParseTreeToExpression(node.ChildNodes[0], typePrefix, ref countTypePrefix1, name);
                arg1 = ParseTreeToExpression(node.ChildNodes[1], typePrefix, ref countTypePrefix1, name);
                arg2 = ParseTreeToExpression(node.ChildNodes[2], typePrefix, ref countTypePrefix2, name);
                upperCountTypePrefix += countTypePrefix1 + countTypePrefix2;
                if (arg1 == "$SKIP" || arg2 == "$SKIP")
                {
                    if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                    {
                        return "true";
                    }
                    return "$SKIP";
                }
                return arg1 + "." + op + "(" + arg2 + ")";
            case "stringComparison":
            case "numericalComparison":
                /*
                case "hexComparison":
                case "boolComparison":
                case "dateTimeComparison":
                case "timeComparison":
                */
                op = ParseTreeToExpression(node.ChildNodes[1], typePrefix, ref countTypePrefix1, name);
                arg1 = ParseTreeToExpression(node.ChildNodes[0], typePrefix, ref countTypePrefix1, name);
                arg2 += ParseTreeToExpression(node.ChildNodes[2], typePrefix, ref countTypePrefix2, name);
                upperCountTypePrefix += countTypePrefix1 + countTypePrefix2;
                if (arg1 == "$SKIP" || arg2 == "$SKIP")
                {
                    if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                    {
                        return "true";
                    }
                    return "$SKIP";
                }
                if (typePrefix == "num()")
                {
                    if (name == "stringComparison")
                    {
                        if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                        {
                            return "true";
                        }
                        return "$SKIP";
                    }
                }
                if (typePrefix == "str()")
                {
                    if (name == "numericalComparison")
                    {
                        if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                        {
                            return "true";
                        }
                        return "$SKIP";
                    }
                }
                return "(" + arg1 + op + arg2 + ")";
            case "eq":
                return " == ";
            case "ne":
                return " != ";
            case "gt":
                return " > ";
            case "ge":
                return " >= ";
            case "lt":
                return " < ";
            case "le":
                return " <= ";
            case "contains":
                return "Contains";
            case "starts-with":
                return "StartsWith";
            case "ends-with":
                return "EndsWith";
            case "regex":
                return "Regex";
        }
        return " $ERROR ";
    }

    public void ParseAccessRules(ParseTreeNode node)
    {
        mySecurityRules.ClearSecurityRules();

        ParseAccessRule(node);
    }
    static List<string> Names = new List<string>();
    static string access = "";
    static string right = "";
    void ParseAccessRule(ParseTreeNode node)
    {
        switch (node.Term.Name)
        {
            case "AccessRule":
                Names = new List<string>();
                access = "";
                right = "";
                foreach (var c in node.ChildNodes)
                {
                    ParseAccessRule(c);
                }
                break;
            case "Access":
                access = node.ChildNodes[0].Term.Name;
                break;
            case "Right":
                right = node.ChildNodes[0].Term.Name;
                break;
            case "stringComparison":
                if (node.ChildNodes[0].Term.Name == "SingleAttribute")
                {
                    var claim = node.ChildNodes[0].ChildNodes[0];
                    if (claim.ChildNodes[1].Token.Value.ToString() == "ROLE")
                    {
                        Names.Add(node.ChildNodes[2].Token.Value.ToString());
                    }

                }
                else
                {
                    string semanticId = node.ChildNodes[2].Token.Value.ToString();
                    if (semanticId != "$DELETE")
                    {
                        foreach (var n in Names)
                        {
                            SecurityRole role = new SecurityRole();

                            role.Name = n;
                            if (access != "ALLOW")
                            {
                                continue;
                            }
                            role.Kind = KindOfPermissionEnum.Allow;
                            if (right != "READ")
                            {
                                continue;
                            }
                            role.Permission = AccessRights.READ;
                            role.ObjectType = "semanticid";
                            role.ApiOperation = "";
                            role.SemanticId = semanticId;
                            role.RulePath = "";

                            mySecurityRules.AddSecurityRule(n, access, right, "semanticid", semanticId);
                        }
                    }
                }
                break;
            default:
                foreach (var c in node.ChildNodes)
                {
                    ParseAccessRule(c);
                }
                break;
        }
    }
}
