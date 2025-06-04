using AasSecurity.Models;
using Irony.Parsing;
using Contracts;
using System.Linq.Expressions;

public class QueryGrammarJSON : Grammar
{
    public QueryGrammarJSON(IContractSecurityRules contractSecurityRules) : base(caseSensitive: true)
    {
        mySecurityRules = contractSecurityRules;

        // Whitespace
        // var ws = new RegexBasedTerminal("ws", @"\s*");
        var whitespaceTerminal = new RegexBasedTerminal("whitespace", @"[ \n\t\r\f]+");
        NonGrammarTerminals.Add(whitespaceTerminal);

        // Define terminals
        var stringLiteral = new StringLiteral("StringLiteral", "\"", StringOptions.AllowsAllEscapes);
        var numberLiteral = new NumberLiteral("NumberLiteral");
        var booleanLiteral = new RegexBasedTerminal("BooleanLiteral", "true|false");

        // Define non-terminals: other Literals
        var hexLiteral = new RegexBasedTerminal("HexLiteral", "\"16#([0-9A-F]+)\"");
        var datetimeLiteral = new NonTerminal("DateTimeLiteral");
        datetimeLiteral.Rule = "\"" + new RegexBasedTerminal("datetime", @"\d{4}-\d{2}-\d{2}(T|\s)\d{2}:\d{2}(:\d{2})?(\.\d+)?(Z|([+-]\d{2}:\d{2}))?") + "\"";
        var timeLiteral = new NonTerminal("TimeLiteral");
        timeLiteral.Rule = ToTerm("\"") + new RegexBasedTerminal("time", @"\d{2}:\d{2}(:\d{2})?(\.\d+)?(Z|([+-]\d{2}:\d{2}))?") + ToTerm("\"");

        // Define non-terminals
        var json = new NonTerminal("json");
        var query = new NonTerminal("query");
        var logicalExpression = new NonTerminal("logical_expression");
        var matchExpression = new NonTerminal("match_expression");
        var comparisonItems = new NonTerminal("comparison_items");
        var stringItems = new NonTerminal("string_items");
        var logicalExpressionArray = new NonTerminal("logical_expression_array");
        var matchExpressionArray = new NonTerminal("match_expression_array");
        var value = new NonTerminal("value");
        var cast = new NonTerminal("cast");
        var stringValue = new NonTerminal("string_value");
        var allAccessPermissionRules = new NonTerminal("all_access_permission_rules");
        var defattributesArray = new NonTerminal("defattributes_array");
        var defattributes = new NonTerminal("defattributes");
        var defaclsArray = new NonTerminal("defacls_array");
        var defacls = new NonTerminal("defacls");
        var defobjectsArray = new NonTerminal("defobjects_array");
        var defobjects = new NonTerminal("defobjects");
        var defformulasArray = new NonTerminal("defformulas_array");
        var defformulas = new NonTerminal("defformulas");
        var accessPermissionRuleArray = new NonTerminal("access_permission_rule_array");
        var accessPermissionRule = new NonTerminal("access_permission_rule");
        var acl = new NonTerminal("acl");
        var attributeArray = new NonTerminal("attribute_array");
        var attribute = new NonTerminal("attribute");
        var stringArray = new NonTerminal("string_array");
        var rightsArray = new NonTerminal("rights_array");
        var objectArray = new NonTerminal("object_array");
        var object_ = new NonTerminal("object");
        var useobjectsArray = new NonTerminal("useobjects_array");
        var rightsEnum = new NonTerminal("rights_enum");
        var accessEnum = new NonTerminal("access_enum");
        var globalEnum = new NonTerminal("global_enum");

        // Define rules
        json.Rule = (query | allAccessPermissionRules);
        // json.Rule = (allAccessPermissionRules);
        // query.Rule = ToTerm("{") + "\"Query\":" + ToTerm("{") + "\"$condition\":" + logicalExpression + (ToTerm(",") + "\"$select\":" + "id").Q() + ToTerm("}") + ToTerm("}");
        query.Rule = ToTerm("{") + "\"Query\":" + ToTerm("{") + "\"$condition\":" + logicalExpression + ToTerm("}") + ToTerm("}");
        logicalExpression.Rule = ToTerm("{") + (("\"$and\":" + logicalExpressionArray) |
                                            ("\"$or\":" + logicalExpressionArray) |
                                            ("\"$not\":" + logicalExpression) |
                                            ("\"$match\":" + matchExpressionArray) |
                                            ("\"$eq\":" + comparisonItems) |
                                            ("\"$ne\":" + comparisonItems) |
                                            ("\"$gt\":" + comparisonItems) |
                                            ("\"$ge\":" + comparisonItems) |
                                            ("\"$lt\":" + comparisonItems) |
                                            ("\"$le\":" + comparisonItems) |
                                            ("\"$contains\":" + stringItems) |
                                            ("\"$starts_with\":" + stringItems) |
                                            ("\"$ends_with\":" + stringItems) |
                                            ("\"$regex\":" + stringItems) |
                                            ("\"$boolean\":" + booleanLiteral))
                                            + ToTerm("}");
        matchExpression.Rule = ToTerm("{") + (("\"$match\":" + matchExpressionArray) |
                                           ("\"$eq\":" + comparisonItems) |
                                           ("\"$ne\":" + comparisonItems) |
                                           ("\"$gt\":" + comparisonItems) |
                                           ("\"$ge\":" + comparisonItems) |
                                           ("\"$lt\":" + comparisonItems) |
                                           ("\"$le\":" + comparisonItems) |
                                           ("\"$contains\":" + stringItems) |
                                           ("\"$starts_with\":" + stringItems) |
                                           ("\"$ends_with\":" + stringItems) |
                                           ("\"$regex\":" + stringItems) |
                                           ("\"$boolean\":" + booleanLiteral))
                                           + ToTerm("}");
        comparisonItems.Rule = "[" + value + ToTerm(",") + value + "]";
        stringItems.Rule = "[" + stringValue + ToTerm(",") + stringValue + "]";
        var logicalExpressionArrayStar = new NonTerminal("__logicalExpressionArrayStar");
        logicalExpressionArrayStar.Rule = MakeStarRule(logicalExpressionArrayStar, ToTerm(",") + logicalExpression);
        logicalExpressionArray.Rule = "[" + logicalExpression + logicalExpressionArrayStar + "]";
        var matchExpressionArrayStar = new NonTerminal("__matchExpressionArrayStar");
        matchExpressionArrayStar.Rule = MakeStarRule(matchExpressionArrayStar, ToTerm(",") + matchExpression);
        matchExpressionArray.Rule = "[" + matchExpression + matchExpressionArrayStar + "]";
        value.Rule = ToTerm("{") + (("\"$field\":" + stringLiteral) |
                                 ("\"$strVal\":" + stringLiteral) |
                                 ("\"$attribute\":" + attribute) |
                                 ("\"$numVal\":" + numberLiteral) |
                                 ("\"$hexVal\":" + hexLiteral) |
                                 ("\"$dateTimeVal\":" + datetimeLiteral) |
                                 ("\"$timeVal\":" + timeLiteral) |
                                 ("\"$boolean\":" + booleanLiteral) |
                                 ("\"$numCast\":" + value) |
                                 ("\"$hexCast\":" + value) |
                                 ("\"$boolCast\":" + value) |
                                 ("\"$dateTimeCast\":" + value) |
                                 ("\"$timeCast\":" + value) |
                                 ("\"$dayOfWeek\":" + value) |
                                 ("\"$dayOfMonth\":" + value) |
                                 ("\"$month\":" + value) |
                                 ("\"$year\":" + value))
                                 + ToTerm("}");
        cast.Rule = ToTerm("{") + (
                                 ("\"$strCast\":" + value) |
                                 ("\"$numCast\":" + value) |
                                 ("\"$hexCast\":" + value) |
                                 ("\"$boolCast\":" + value) |
                                 ("\"$dateTimeCast\":" + value) |
                                 ("\"$timeCast\":" + value))
                                 + ToTerm("}");
        stringValue.Rule = ToTerm("{") + (("\"$field\":" + stringLiteral) |
                                 ("\"$strVal\":" + stringLiteral) |
                                 ("\"$strCast\":" + value) |
                                 ("\"$attribute\":" + attribute))
                                 + ToTerm("}");
        var defattributesArrayStar = new NonTerminal("__defattributesArrayStar");
        defattributesArrayStar.Rule = MakeStarRule(defattributesArrayStar, "\"DEFATTRIBUTES\":" + defattributesArray + ToTerm(","));
        var defaclsArrayStar = new NonTerminal("__defaclsArrayStar");
        defaclsArrayStar.Rule = MakeStarRule(defaclsArrayStar, "\"DEFACLS\":" + defaclsArray + ToTerm(","));
        var defobjectsArrayStar = new NonTerminal("__defobjectsArrayStar");
        defobjectsArrayStar.Rule = MakeStarRule(defobjectsArrayStar, "\"DEFOBJECTS\":" + defobjectsArray + ToTerm(","));
        var defformulasArrayStar = new NonTerminal("__defformulasArrayStar");
        defformulasArrayStar.Rule = MakeStarRule(defformulasArrayStar, "\"DEFFORMULAS\":" + defformulasArray + ToTerm(","));
        allAccessPermissionRules.Rule = ToTerm("{") + "\"AllAccessPermissionRules\":" + ToTerm("{") +
                                        defattributesArrayStar +
                                        defaclsArrayStar +
                                        defobjectsArrayStar +
                                        defformulasArrayStar +
                                        "\"rules\":" + accessPermissionRuleArray + ToTerm("}") + ToTerm("}");
        defattributesArray.Rule = "[" + defattributes + (ToTerm(",") + defattributes).Q() + "]";
        defattributes.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + "\"attributes\":" + attributeArray + ToTerm("}");
        defaclsArray.Rule = "[" + defacls + (ToTerm(",") + defacls).Q() + "]";
        defacls.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + "\"acl\":" + acl + ToTerm("}");
        defobjectsArray.Rule = "[" + defobjects + (ToTerm(",") + defobjects).Q() + "]";
        defobjects.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + (("\"objects\":" + objectArray) |
                                                                                         ("\"USEOBJECTS\":" + useobjectsArray)) + ToTerm("}");
        defformulasArray.Rule = "[" + defformulas + (ToTerm(",") + defformulas).Q() + "]";
        defformulas.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + "\"formula\":" + logicalExpression + ToTerm("}");
        accessPermissionRuleArray.Rule = ToTerm("[") + accessPermissionRule + (ToTerm(",") + accessPermissionRule).Q() + "]";
        accessPermissionRule.Rule = ToTerm("{") + (("\"ACL\":" + acl) |
                                                ("\"USEACL\":" + stringLiteral)) +
                                    ((ToTerm(",") + "\"OBJECTS\":" + objectArray) |
                                    (ToTerm(",") + "\"USEOBJECTS\":" + stringArray)) +
                                    ((ToTerm(",") + "\"FORMULA\":" + logicalExpression) |
                                     (ToTerm(",") + "\"USEFORMULA\":" + stringLiteral)) +
                                    ((ToTerm(",") + "\"FRAGMENT\":" + stringLiteral).Q()) +
                                    (((ToTerm(",") + "\"FILTER\":" + logicalExpression) |
                                     (ToTerm(",") + "\"USEFILTER\":" + stringLiteral)).Q())
                                     + ToTerm("}");
        acl.Rule = ToTerm("{") + (("\"ATTRIBUTES\":" + attributeArray) |
                               ("\"USEATTRIBUTES\":" + stringArray)) +
                           ToTerm(",") + "\"RIGHTS\":" + rightsArray +
                           ToTerm(",") + "\"ACCESS\":" + accessEnum + ToTerm("}");
        attributeArray.Rule = "[" + attribute + (ToTerm(",") + attribute).Q() + "]";
        attribute.Rule = ToTerm("{") + (("\"CLAIM\":" + stringLiteral) |
                                     ("\"GLOBAL\":" + globalEnum) |
                                     ("\"REFERENCE\":" + stringLiteral)) + ToTerm("}");
        stringArray.Rule = "[" + stringLiteral + (ToTerm(",") + stringLiteral).Q() + "]";
        rightsArray.Rule = "[" + rightsEnum + (ToTerm(",") + rightsEnum).Q() + (ToTerm(",") + rightsEnum).Q() + (ToTerm(",") + rightsEnum).Q() + "]";
        objectArray.Rule = "[" + object_ + (ToTerm(",") + object_).Q() + "]";
        object_.Rule = ToTerm("{") + (("\"ROUTE\":" + stringLiteral) |
                                  ("\"IDENTIFIABLE\":" + stringLiteral) |
                                  ("\"REFERABLE\":" + stringLiteral) |
                                  ("\"FRAGMENT\":" + stringLiteral) |
                                  ("\"DESCRIPTOR\":" + stringLiteral)) + ToTerm("}");
        useobjectsArray.Rule = "[" + stringLiteral + (ToTerm(",") + stringLiteral).Q() + "]";
        rightsEnum.Rule = ToTerm("\"CREATE\"") | "\"READ\"" | "\"UPDATE\"" | "\"DELETE\"" | "\"EXECUTE\"" | "\"VIEW\"" | "\"ALL\"" | "\"TREE\"";
        accessEnum.Rule = ToTerm("\"ALLOW\"") | "\"DISABLED\"";
        globalEnum.Rule = ToTerm("\"LOCALNOW\"") | "\"UTCNOW\"" | "\"CLIENTNOW\"" | "\"ANONYMOUS\"";

        // Set the root
        this.Root = json;

        // Define punctuation and transient terms
        MarkPunctuation(ToTerm("{"), ToTerm("}"), ToTerm(","), ToTerm("["), ToTerm("]"), ToTerm("?"), ToTerm("\""));
        // MarkTransient(json, query, logicalExpression, matchExpression, comparisonItems, stringItems, logicalExpressionArray, matchExpressionArray, value, attribute, allAccessPermissionRules, defattributesArray, defattributes, defaclsArray, defacls, defobjectsArray, defobjects, defformulasArray, defformulas, accessPermissionRuleArray, accessPermissionRule, acl, attributeArray, stringArray, rightsArray, objectArray, useobjectsArray);
        MarkTransient(logicalExpressionArrayStar, matchExpressionArrayStar);

        // Register operators
        RegisterOperators(1, "AND", "OR");
        RegisterOperators(2, "NOT");
    }

    private IContractSecurityRules mySecurityRules;

    public string idShortPath = "";

    static void PrintParseTree(ParseTreeNode node, int indent, StringWriter sw)
    {
        if (node == null)
            return;

        if (skip.Contains(node.Term.Name))
            return;

        string text = "";
        bool starts = false;
        foreach (var s in startsWith)
        {
            starts |= node.Term.Name.StartsWith(s);
        }

        if (starts)
        {
            foreach (var c in node.ChildNodes)
            {
                PrintParseTree(c, indent, sw);
            }
        }
        else
        {
            if (!starts)
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

    public class treeNode
    {
        public string Name = "";
        public string Value = "";
        public string Type = "";
        public List<treeNode> Children = null;
        public treeNode Parent = null;
        public string idShortPath = "";
    }

    static List<string> skip = new List<string>() { "?", "\"Query\":", "\"$condition\":" };
    static List<string> startsWith = new List<string>() { "Unnamed", "__", "query" };
    static List<string> keep1 = new List<string>() { "\"$and\":", "\"$or\":", "\"$not\":" };

    private void setParent(treeNode parent, List<treeNode> Children)
    {
        foreach (var c in Children)
        {
            c.Parent = parent;
        }
    }
    private bool starts(string name)
    {
        var starts = false;
        foreach (var s in startsWith)
        {
            if (name.StartsWith(s))
            {
                return true;
            }
        }

        return false;
    }
    public treeNode simplifyTree(ParseTreeNode node)
    {
        var tn = new treeNode();
        tn.Name = node.Term.Name;
        tn.Children = new List<treeNode>();
        if (node.Token != null && node.Token.Value != null)
        {
            tn.Value = node.Token.Value.ToString();
        }

        foreach (var c in node.ChildNodes)
        {
            if (skip.Contains(c.Term.Name))
                continue;

            var tc = simplifyTree(c);
            if (tc != null)
            {
                tc.Parent = tn;
                tn.Children.Add(tc);
            }
        }

        if (tn.Children.Count == 2 && starts(tn.Name))
        {
            // Unnamed expression
            if (tn.Children[1].Name == "logical_expression_array")
            {
                tn.Name = tn.Children[0].Name;
                tn.Type = tn.Children[1].Type;
                tn.Children = tn.Children[1].Children;
                setParent(tn, tn.Children);
            }
            if (tn.Name.StartsWith("Unnamed") && tn.Children[1].Name.StartsWith("\"$"))
            {
                // nested expression with 2 operators
                tn.Name = tn.Children[0].Name;
                tn.Type = tn.Children[1].Type;
                tn.Children.RemoveAt(0);
            }
        }

        if (tn.Children.Count == 1 && !keep1.Contains(tn.Name))
        {
            if (!starts(tn.Children[0].Name))
            {
                tn.Name = tn.Children[0].Name;
                tn.Value = tn.Children[0].Value;
            }

            tn.Type = tn.Children[0].Type;
            tn.Children = tn.Children[0].Children;
            setParent(tn, tn.Children);
        }

        switch (tn.Name)
        {
            case "logical_expression_array":
                // Add Children of logical_expression_array_star
                if (tn.Children[1].Name == "__logicalExpressionArrayStar")
                {
                    var cc = tn.Children[1].Children;
                    tn.Children.RemoveAt(1);
                    setParent(tn, cc);
                    tn.Children.AddRange(cc);
                }
                break;
            case "logical_expression":
                // Operator
                tn.Name = tn.Children[0].Name;
                if (tn.Name == "\"$not\":")
                {
                    tn.Type = tn.Children[1].Type;
                    tn.idShortPath = tn.Children[1].idShortPath;
                    tn.Children.RemoveAt(0);
                }
                else if (tn.Name == "\"$boolean\":")
                {
                    tn.Type = tn.Children[1].Type;
                    tn.Value = tn.Children[1].Value;
                    tn.Children.Clear();
                }
                else
                {
                    // Add Children of logical_expression_array
                    tn.Type = tn.Children[1].Type;
                    tn.Children = tn.Children[1].Children;
                    foreach (var c in tn.Children)
                    {
                        if (c.idShortPath != "")
                        {
                            if (tn.idShortPath != "" && tn.idShortPath != c.idShortPath)
                            {
                                tn.idShortPath = "$ERROR";
                            }
                            tn.idShortPath = c.idShortPath;
                        }
                    }
                    setParent(tn, tn.Children);
                }
                // Add path expression
                if (tn.idShortPath != "")
                {
                    var tn_path = new treeNode();
                    tn_path.Children = new List<treeNode>();
                    tn_path.Name = "\"$path\":";
                    tn_path.Type = "str";
                    tn_path.Value = tn.idShortPath;
                    tn_path.Children.Add(tn);
                    tn = tn_path;

                    /*
                    var tn_and = new treeNode();
                    tn_and.Children = new List<treeNode>();
                    tn_and.Name = "\"$and\":";
                    tn_and.Children.Add(tn_path);
                    tn_and.Children.Add(tn);
                    setParent(tn_and, tn.Children);
                    tn = tn_and;
                    break;
                    */
                }
                /*
                if (tn.Name == "\"$eq\":" && tn.Children.Count == 2 && tn.Children[0].Type == "field")
                {
                    if (tn.Children[0].Value == "$sme#path")
                    {
                        idShortPath = tn.Children[1].Value;
                        return null; // skip expression
                    }
                }
                */
                break;
            case "value":
            case "string_value":
                tn.Name = tn.Children[0].Name;
                tn.Type = tn.Children[0].Type;
                tn.Value = tn.Children[1].Value;
                tn.Children.Clear();
                if (tn.Name == "\"$field\":")
                {
                    var str1 = tn.Value.Split("#");
                    if (str1[0].Contains('.'))
                    {
                        var str2 = str1[0].Split(".");
                        tn.idShortPath = str2[1];
                        for (int i = 2; i < str2.Count(); i++)
                        {
                            tn.idShortPath += "." + str2[i];
                        }
                        tn.Value = str2[0] + "." + str1[1];
                    }
                }
                break;
            case "\"$strVal\":":
            case "StringLiteral":
                tn.Type = "str";
                break;
            case "\"$numVal\":":
            case "NumericalLiteral":
                tn.Type = "num";
                break;
            case "BooleanLiteral":
                tn.Type = "bool";
                break;
            case "\"$field\":":
                tn.Type = "field";
                break;
            default:
                break;
        }

        // Detect type of subtree
        if (tn.Type == "" && tn.Children.Count != 0)
        {
            foreach (var c in tn.Children)
            {
                if (tn.Type == "")
                {
                    tn.Type = c.Type;
                }
                else if (tn.Type == "field" && c.Type != "field")
                {
                    tn.Type = c.Type;
                }
            }
        }

        return tn;
    }
    public string ParseTreeToExpressionWithAccessRules(ParseTreeNode node, string typePrefix, ref int upperCountTypePrefix, string parentType = "")
    {
        var result = ParseTreeToExpression(node, typePrefix, ref upperCountTypePrefix, parentType);

        if (accessRuleNode == null && accessRuleExpression.TryGetValue("all", out _))
        {
            switch (typePrefix)
            {
                case "":
                    result = $"({accessRuleExpression})&&({result})";
                    break;
                case "sm.":
                    result = result.Replace("$SKIP", "true");
                    var accessSubmodel = accessRuleExpression["all"].Replace("sm.", "");
                    result = $"({accessSubmodel})&&({result})";
                    break;
            }
        }
        return result;
    }
    public string ParseTreeToExpression(ParseTreeNode node, string typePrefix, ref int upperCountTypePrefix, string parentType = "")
    {
        // simplify parse tree
        var combinedTree = simplifyTree(node);

        // combine with access rules
        if (accessRuleNode != null)
        {
            var tn = new treeNode();
            tn.Children = new List<treeNode>();
            tn.Name = "\"$and\":";
            tn.Children.Add(accessRuleNode);
            tn.Children.Add(combinedTree);
            setParent(tn, tn.Children);
            combinedTree = tn;
        }

        var expression = ParseTreeToExpression(combinedTree, typePrefix, ref upperCountTypePrefix, parentType);

        return expression;
    }

    public string ParseTreeToExpression(treeNode node, string typePrefix, ref int upperCountTypePrefix, string parentType = "")
    {
        var expression = ParseTreeToExpressionRaw(node, typePrefix, ref upperCountTypePrefix, parentType);

        expression = expression.Replace("$TRUE", "true");
        expression = expression.Replace("$FALSE", "false");
        while (expression.Contains("true&&true") || expression.Contains("true||true") || expression.Contains("(true)")
            || expression.Contains("false&&false") || expression.Contains("false||false") || expression.Contains("(false)")
            || expression.Contains("true&&false") || expression.Contains("true||false")
            || expression.Contains("false&&true") || expression.Contains("false||true")
            || expression.Contains("!true") || expression.Contains("!false")
            )
        {
            expression = expression.Replace("true&&true", "true");
            expression = expression.Replace("true||true", "true");
            expression = expression.Replace("(true)", "true");
            expression = expression.Replace("false&&false", "false");
            expression = expression.Replace("false||false", "false");
            expression = expression.Replace("(false)", "false");
            expression = expression.Replace("true&&false", "false");
            expression = expression.Replace("true||false", "true");
            expression = expression.Replace("false&&true", "true");
            expression = expression.Replace("false||true", "false");
            expression = expression.Replace("!true", "false");
            expression = expression.Replace("!false", "true");
        }
        return expression;
    }
    public string ParseTreeToExpressionRaw(treeNode node, string typePrefix, ref int upperCountTypePrefix, string parentType = "")
    {
        upperCountTypePrefix = 0;
        var countTypePrefix1 = 0;
        var countTypePrefix2 = 0;
        var op = "";
        var arg1 = "";
        var arg2 = "";
        var pattern = "";

        var name = node.Name;
        var value = node.Value;
        var type = node.Type;
        var children = node.Children;

        switch (name)
        {
            case "\"$and\":":
                op = "&&";
                pattern = "andor";
                break;
            case "\"$or\":":
                op = "||";
                pattern = "andor";
                break;
            case "\"$not\":":
                op = "!";
                pattern = "not";
                break;
            case "\"$boolean\":":
                op = "!";
                pattern = "bool";
                break;
            case "\"$eq\":":
                op = "==";
                pattern = "compare";
                break;
            case "\"$ne\":":
                op = "!=";
                pattern = "compare";
                break;
            case "\"$gt\":":
                op = ">";
                pattern = "compare";
                break;
            case "\"$ge\":":
                op = ">=";
                pattern = "compare";
                break;
            case "\"$lt\":":
                op = "<";
                pattern = "compare";
                break;
            case "\"$le\":":
                op = "<=";
                pattern = "compare";
                break;
            case "\"$contains\":":
                op = "Contains";
                pattern = "string";
                break;
            case "\"$starts-with\":":
            case "\"$starts_with\":":
                op = "StartsWith";
                pattern = "string";
                break;
            case "\"ends-with\":":
            case "\"ends_with\":":
                op = "EndsWith";
                pattern = "string";
                break;
            case "\"$strVal\":":
                pattern = "strval";
                break;
            case "\"$numVal\":":
                pattern = "numval";
                break;
            case "\"$field\":":
                pattern = "field";
                break;
            case "\"$path\":":
                pattern = "path";
                break;
        }

        switch (pattern)
        {
            case "bool":
                return node.Value;
            case "not":
                arg1 = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1);
                upperCountTypePrefix += countTypePrefix1;
                if (arg1 == "$SKIP")
                {
                    return "$SKIP";
                }
                if (arg1 == "$TRUE")
                {
                    return "$TRUE";
                }
                return "!" + arg1;
            case "andor":
                List<string> args = new List<string>();
                List<int> count = new List<int>();
                args.Add(ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1, node.Type));
                count.Add(countTypePrefix1);
                for (int i = 1; i < node.Children.Count; i++)
                {
                    countTypePrefix2 = 0;
                    args.Add(ParseTreeToExpression(node.Children[i], typePrefix, ref countTypePrefix2, node.Type));
                    count.Add(countTypePrefix2);
                    countTypePrefix1 += countTypePrefix2;
                }
                upperCountTypePrefix += countTypePrefix1;

                int skipCount = 0;
                int trueCount = 0;
                int falseCount = 0;

                foreach (var a in args)
                {
                    switch (a)
                    {
                        case "$SKIP":
                            skipCount++;
                            break;
                        case "$TRUE":
                            trueCount++;
                            break;
                        case "$FALSE":
                            falseCount++;
                            break;
                    }
                }

                if (skipCount == args.Count)
                {
                    if (countTypePrefix1 != 0)
                    {
                        return "$TRUE";
                    }
                    return "$SKIP";
                }
                if (skipCount != 0)
                {
                    int argsCount = args.Count;
                    for (int i = 0; i < argsCount; i++)
                    {
                        if (args[i] == "$SKIP")
                        {
                            if (count[i] != 0)
                            {
                                return "$TRUE";
                            }
                            args.RemoveAt(i);
                            count.RemoveAt(i);
                            argsCount--;
                            i--;
                        }
                        else
                        {
                            // constant
                            if ((op == "&&" && args[i] == "true") || (op == "||" && args[i] == "false"))
                            {
                                args.RemoveAt(i);
                                count.RemoveAt(i);
                                argsCount--;
                                i--;
                            }
                        }
                    }
                }

                if (args.Count == 0)
                {
                    return "$SKIP";
                }
                // and
                if (op == "&&")
                {
                    if (trueCount == args.Count)
                    {
                        return "$TRUE";
                    }
                    if (falseCount != 0)
                    {
                        return "$FALSE";
                    }
                }
                // or
                if (op == "||")
                {
                    op = "||";
                    if (trueCount != 0)
                    {
                        return "$TRUE";
                    }
                    if (falseCount == args.Count)
                    {
                        return "$FALSE";
                    }
                }

                var expression = args[0];
                for (int i = 1; i < args.Count; i++)
                {
                    expression += op + args[i];
                }
                expression = "(" + expression + ")";
                return expression;
            case "compare":
            case "string":
                arg1 = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1, node.Type);
                arg2 = ParseTreeToExpression(node.Children[1], typePrefix, ref countTypePrefix2, node.Type);
                upperCountTypePrefix += countTypePrefix1 + countTypePrefix2;
                if (arg1 == "$SKIP" || arg2 == "$SKIP")
                {
                    if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                    {
                        // return "true";
                    }
                    return "$SKIP";
                }

                var argumentType1 = node.Children[0].Type;
                var argumentType2 = node.Children[1].Type;
                if (typePrefix == "num()" && (argumentType1 == "str" || argumentType2 == "str"))
                {
                    return "$SKIP";
                }
                if (typePrefix == "str()" && (argumentType1 == "num" || argumentType2 == "num"))
                {
                    return "$SKIP";
                }
                if (typePrefix == "" && (arg1 == "sme.value" || arg2 == "sme.value"))
                {
                    string change = "";

                    if (argumentType1 == "num" || argumentType2 == "num")
                    {
                        change = "mvalue";
                    }
                    else
                    {
                        if (pattern == "string" || argumentType1 == "str" || argumentType2 == "str")
                        {
                            change = "svalue";
                        }
                    }
                    if (change != "")
                    {
                        if (arg1 == "sme.value")
                        {
                            arg1 = change;
                        }
                        if (arg2 == "sme.value")
                        {
                            arg2 = change;
                        }
                    }
                }
                if (pattern == "string")
                {
                    return arg1 + "." + op + "(" + arg2 + ")";
                }
                return "(" + arg1 + op + arg2 + ")";
            case "strval":
                if (typePrefix == "num()")
                {
                    return "$SKIP";
                }
                return "\"" + value + "\"";
            case "numval":
                if (typePrefix == "str()")
                {
                    return "$SKIP";
                }
                return value;
            case "field":
                value = value.Replace("$", "");
                value = value.Replace("#", ".");
                if (value == "sm.id") // patch grammar vs database
                {
                    value = "sm.identifier";
                }
                // complete expression for joined tables
                if (typePrefix == "" && value == "sme.value")
                {
                    switch (parentType)
                    {
                        case "str":
                            return "svalue";
                        case "num":
                            return "mvalue";
                        default:
                            return "$ERROR";
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
                    var result = value.Replace("sme.", "");
                    return result;
                }
                // string value table and num value table
                if (typePrefix == "str()" || typePrefix == "num()")
                {
                    if (value != "sme.value")
                    {
                        return "$SKIP";
                    }
                    upperCountTypePrefix++;
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
            case "path":
                arg1 = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1);
                upperCountTypePrefix += countTypePrefix1;

                if (typePrefix != "")
                {
                    return arg1;
                }

                if (arg1 == "$SKIP")
                {
                    return "$SKIP";
                }
                if (arg1 == "$TRUE")
                {
                    return "$TRUE";
                }
                if (arg1 == "true")
                {
                    return "true";
                }
                return "$$path$$" + value + "$$" + arg1 + "$$";
            case "StringLiteral":
                if (parentType == "num")
                {
                    return value;
                }
                return "\"" + value + "\"";
            case "NumericalLiteral":
                if (parentType == "str")
                {
                    return "\"" + value + "\"";
                }
                return value;
            case "logicalAndExpression":
            case "logicalOrExpression":
                arg1 = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1);
                arg2 = ParseTreeToExpression(node.Children[1], typePrefix, ref countTypePrefix2);
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
                arg1 = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1);
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
                op = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1, name);
                arg1 = ParseTreeToExpression(node.Children[1], typePrefix, ref countTypePrefix1, name);
                arg2 = ParseTreeToExpression(node.Children[2], typePrefix, ref countTypePrefix2, name);
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
                op = ParseTreeToExpression(node.Children[1], typePrefix, ref countTypePrefix1, name);
                arg1 = ParseTreeToExpression(node.Children[0], typePrefix, ref countTypePrefix1, name);
                arg2 += ParseTreeToExpression(node.Children[2], typePrefix, ref countTypePrefix2, name);
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
            case "$eq":
                return " == ";
            case "$ne":
                return " != ";
            case "$gt":
                return " > ";
            case "$ge":
                return " >= ";
            case "$lt":
                return " < ";
            case "$le":
                return " <= ";
            case "$contains":
                return "Contains";
            case "$starts-with":
                return "StartsWith";
            case "$ends-with":
                return "EndsWith";
            case "$regex":
                return "Regex";
        }
        return " $NOT_IMPLEMENTED ";
    }

    public static new List<Dictionary<string, string>> allAccessRuleExpressions = [];
    public static new Dictionary<string, string> accessRuleExpression = [];
    // public static string accessRuleExpression = "((sm.idShort==\"Nameplate\")||(sm.idShort==\"TechnicalData\"))";
    // public static string accessRuleExpression = "(sm.idShort==\"Nameplate\")";
    public static treeNode accessRuleNode = null;
    public void ParseAccessRules(ParseTreeNode node)
    {
        // mySecurityRules.ClearSecurityRules();
        allAccessRuleExpressions = new List<Dictionary<string, string>>();
        accessRuleExpression = new Dictionary<string, string>();

        ParseAccessRule(node);
        if (accessRuleExpression.Count != 0)
        {
            allAccessRuleExpressions.Add(accessRuleExpression);
        }
    }

    static List<string> Names = new List<string>();
    static string access = "";
    static string rights = "";
    static string global = "";
    static bool isClaim = false;
    static string claim = "";
    static bool filter = false;
    static bool route = false;
    void ParseAccessRule(ParseTreeNode node)
    {
        switch (node.Term.Name)
        {
            case "access_permission_rule":
                if (accessRuleExpression.Count != 0)
                {
                    allAccessRuleExpressions.Add(accessRuleExpression);
                }
                accessRuleExpression = new Dictionary<string, string>();
                access = "";
                rights = "";
                global = "";
                isClaim = false;
                claim = "";
                filter = false;
                route = false;

                foreach (var c in node.ChildNodes)
                {
                    ParseAccessRule(c);
                }
                break;
            case "logical_expression":
                var tn = simplifyTree(node);
                accessRuleNode = tn;
                var count = 0;
                if (!filter)
                {
                    var expression = "";
                    accessRuleExpression["all"] = ParseTreeToExpression(tn, "", ref count);
                    expression = ParseTreeToExpression(tn, "sm.", ref count);
                    if (expression == "$SKIP")
                    {
                        expression = "";
                    }
                    accessRuleExpression["sm."] = expression;
                    expression = ParseTreeToExpression(tn, "sme.", ref count);
                    if (expression == "$SKIP")
                    {
                        expression = "";
                    }
                    accessRuleExpression["sme."] = expression;
                    accessRuleExpression["claim"] = claim;
                    accessRuleExpression["right"] = rights;
                }
                else
                {
                    accessRuleExpression["filter"] = ParseTreeToExpression(tn, "", ref count);
                }
                break;
            case "\"FOMRULA\":":
                filter = false;
                break;
            case "\"FILTER\":":
                filter = true;
                break;
            case "\"ROUTE\":":
                route = true;
                break;
            case "\"CLAIM\":":
                isClaim = true;
                break;
            case "StringLiteral":
                if (isClaim)
                {
                    claim = (string)node.Token.Value;
                    isClaim = false;
                }
                else
                {
                    if (route)
                    {
                        route = false;
                        var split = rights.Split(' ');
                        foreach (var s in split)
                        {
                            if (s != "")
                            {
                                mySecurityRules.AddSecurityRule(claim, "ALLOW", s, "api", "", (string)node.Token.Value);
                            }
                        }
                    }
                }
                break;
            case "global_enum":
                global = node.ChildNodes[0].Term.Name;
                break;
            case "rights_enum":
                var r = node.ChildNodes[0].Term.Name;
                r = r.Replace("\"", "");
                rights += r + " ";
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
