/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using AasSecurity.Models;
using Irony.Parsing;
using Contracts;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
// using Newtonsoft.Json.Schema;
// using NJsonSchema;
// using NJsonSchema.Validation;
// using Json.Schema;
using System.Data;
using System.Xml.Linq;
using System.Text.Json;
using NJsonSchema.Validation;
using System.Text.Json.Nodes;

public class QueryGrammarJSON : Grammar
{
    public QueryGrammarJSON(IContractSecurityRules contractSecurityRules) : base(caseSensitive: true)
    {
        mySecurityRules = contractSecurityRules;

        // Whitespace
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
        var selectLiteralId = ToTerm("\"id\"");
        var selectLiteralMatch = ToTerm("\"match\"");
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

        query.Rule = ToTerm("{") + "\"Query\":" + ToTerm("{") +
            (ToTerm("\"$select\":") + (selectLiteralId | selectLiteralMatch) + ToTerm(",")).Q() +
            "\"$condition\":" + logicalExpression +
            (ToTerm(",") + "\"$filter\":" + logicalExpression).Q() +
            ToTerm("}") + ToTerm("}");

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
                                            ("\"$starts-with\":" + stringItems) |
                                            ("\"$ends-with\":" + stringItems) |
                                            ("\"$regex\":" + stringItems) |
                                            ("\"$boolean\":" + booleanLiteral))
                                            + ToTerm("}");
        matchExpression.Rule = ToTerm("{") + ( /* ("\"$match\":" + matchExpressionArray) | nested match currently not implemented */
                                           ("\"$eq\":" + comparisonItems) |
                                           ("\"$ne\":" + comparisonItems) |
                                           ("\"$gt\":" + comparisonItems) |
                                           ("\"$ge\":" + comparisonItems) |
                                           ("\"$lt\":" + comparisonItems) |
                                           ("\"$le\":" + comparisonItems) |
                                           ("\"$contains\":" + stringItems) |
                                           ("\"$starts-with\":" + stringItems) |
                                           ("\"$ends-with\":" + stringItems) |
                                           ("\"$regex\":" + stringItems) |
                                           ("\"$boolean\":" + booleanLiteral))
                                           + ToTerm("}");
        var comparisonPair = new NonTerminal("comparison_pair");
        comparisonPair.Rule = value + ToTerm(",") + value;
        comparisonItems.Rule = "[" + comparisonPair + "]";

        var stringPair = new NonTerminal("string_pair");
        stringPair.Rule = stringValue + ToTerm(",") + stringValue;
        stringItems.Rule = "[" + stringPair + "]";

        var logicalExpressionList = new NonTerminal("logical_expression_list");
        logicalExpressionList.Rule = MakePlusRule(logicalExpressionList, ToTerm(","), logicalExpression);
        logicalExpressionArray.Rule = "[" + logicalExpressionList + "]";

        var matchExpressionList = new NonTerminal("match_expression_list");
        matchExpressionList.Rule = MakePlusRule(matchExpressionList, ToTerm(","), matchExpression);
        matchExpressionArray.Rule = "[" + matchExpressionList + "]";

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

        var defattributesList = new NonTerminal("defattributes_list");
        defattributesList.Rule = MakeStarRule(defattributesList, ToTerm(","), defattributes);
        defattributesArray.Rule = "[" + defattributesList + "]";

        var defaclsList = new NonTerminal("defacls_list");
        defaclsList.Rule = MakeStarRule(defaclsList, ToTerm(","), defacls);
        defaclsArray.Rule = "[" + defaclsList + "]";

        var defobjectsList = new NonTerminal("defobjects_list");
        defobjectsList.Rule = MakeStarRule(defobjectsList, ToTerm(","), defobjects);
        defobjectsArray.Rule = "[" + defobjectsList + "]";

        var defformulasList = new NonTerminal("defformulas_list");
        defformulasList.Rule = MakeStarRule(defformulasList, ToTerm(","), defformulas);
        defformulasArray.Rule = "[" + defformulasList + "]";

        allAccessPermissionRules.Rule = ToTerm("{") + "\"AllAccessPermissionRules\":" + ToTerm("{") +
                                        defattributesArray.Q() +
                                        defaclsArray.Q() +
                                        defobjectsArray.Q() +
                                        defformulasArray.Q() +
                                        "\"rules\":" + accessPermissionRuleArray + ToTerm("}") + ToTerm("}");
        defattributes.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + "\"attributes\":" + attributeArray + ToTerm("}");
        defacls.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + "\"acl\":" + acl + ToTerm("}");
        defobjects.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + (("\"objects\":" + objectArray) |
                                                                                         ("\"USEOBJECTS\":" + useobjectsArray)) + ToTerm("}");
        defformulas.Rule = ToTerm("{") + "\"name\":" + stringLiteral + ToTerm(",") + "\"formula\":" + logicalExpression + ToTerm("}");
        var accessPermissionRuleList = new NonTerminal("access_permission_rule_list");
        accessPermissionRuleList.Rule = MakeStarRule(accessPermissionRuleList, ToTerm(","), accessPermissionRule);
        accessPermissionRuleArray.Rule = "[" + accessPermissionRuleList + "]";

        var filterObject = new NonTerminal("filterObject");
        filterObject.Rule = ToTerm("{") +
                             "\"FRAGMENT\":" + stringLiteral + ToTerm(",") +
                             "\"CONDITION\":" + logicalExpression +
                             ToTerm("}");

        accessPermissionRule.Rule = ToTerm("{") + (("\"ACL\":" + acl) |
                                                ("\"USEACL\":" + stringLiteral)) +
                                    ((ToTerm(",") + "\"OBJECTS\":" + objectArray) |
                                    (ToTerm(",") + "\"USEOBJECTS\":" + stringArray)) +
                                    ((ToTerm(",") + "\"FORMULA\":" + logicalExpression) |
                                     (ToTerm(",") + "\"USEFORMULA\":" + stringLiteral)) +
                                    (((ToTerm(",") + "\"FILTER\":" + filterObject) |
                                     (ToTerm(",") + "\"USEFILTER\":" + stringLiteral)).Q())
                                     + ToTerm("}");
        acl.Rule = ToTerm("{") + (("\"ATTRIBUTES\":" + attributeArray) |
                               ("\"USEATTRIBUTES\":" + stringArray)) +
                           ToTerm(",") + "\"RIGHTS\":" + rightsArray +
                           ToTerm(",") + "\"ACCESS\":" + accessEnum + ToTerm("}");
        var attributeList = new NonTerminal("attribute_list");
        attributeList.Rule = MakeStarRule(attributeList, ToTerm(","), attribute);
        attributeArray.Rule = "[" + attributeList + "]";
        attribute.Rule = ToTerm("{") + (("\"CLAIM\":" + stringLiteral) |
                                     ("\"GLOBAL\":" + globalEnum) |
                                     ("\"REFERENCE\":" + stringLiteral)) + ToTerm("}");
        var stringList = new NonTerminal("string_list");
        stringList.Rule = MakeStarRule(stringList, ToTerm(","), stringLiteral);
        stringArray.Rule = "[" + stringList + "]";
        var rightsList = new NonTerminal("rights_list");
        rightsList.Rule = MakeStarRule(rightsList, ToTerm(","), rightsEnum);
        rightsArray.Rule = "[" + rightsList + "]";
        var objectList = new NonTerminal("object_list");
        objectList.Rule = MakeStarRule(objectList, ToTerm(","), object_);
        objectArray.Rule = "[" + objectList + "]";
        object_.Rule = ToTerm("{") + (("\"ROUTE\":" + stringLiteral) |
                                  ("\"IDENTIFIABLE\":" + stringLiteral) |
                                  ("\"REFERABLE\":" + stringLiteral) |
                                  ("\"FRAGMENT\":" + stringLiteral) |
                                  ("\"DESCRIPTOR\":" + stringLiteral)) + ToTerm("}");
        var useobjectsList = new NonTerminal("useobjects_list");
        useobjectsList.Rule = MakeStarRule(useobjectsList, ToTerm(","), stringLiteral);
        useobjectsArray.Rule = "[" + useobjectsList + "]";
        rightsEnum.Rule = ToTerm("\"CREATE\"") | "\"READ\"" | "\"UPDATE\"" | "\"DELETE\"" | "\"EXECUTE\"" | "\"VIEW\"" | "\"ALL\"" | "\"TREE\"";
        accessEnum.Rule = ToTerm("\"ALLOW\"") | "\"DISABLED\"";
        globalEnum.Rule = ToTerm("\"LOCALNOW\"") | "\"UTCNOW\"" | "\"CLIENTNOW\"" | "\"ANONYMOUS\"";

        // Set the root
        this.Root = json;

        // Define punctuation and transient terms
        MarkPunctuation(ToTerm("{"), ToTerm("}"), ToTerm(","), ToTerm("["), ToTerm("]"), ToTerm("?"), ToTerm("\""));
        // MarkTransient(logicalExpressionList, matchExpressionList, comparisonPair, stringPair, attributeList, stringList, rightsList, objectList, useobjectsList);
        MarkTransient(logicalExpressionArray, matchExpressionArray, defattributesArray, defaclsArray, defobjectsArray, defformulasArray,
            accessPermissionRuleArray, attributeArray, stringArray, rightsArray, objectArray, useobjectsArray,
            comparisonItems);

        // Register operators
        RegisterOperators(1, "AND", "OR");
        RegisterOperators(2, "NOT");
    }

    private IContractSecurityRules mySecurityRules;

    public string idShortPath = "";

    public static string createExpression(string mode, object? obj, string type = "", string smeValue = "")
    {
        if (obj == null)
            return "";

        if (type == "")
        {
            switch (obj)
            {
                case Root r:
                    createExpression(mode, r.Query);
                    createExpression(mode, r.AllAccessPermissionRules);
                    break;
                case AllAccessPermissionRules all:
                    foreach (var rule in all.Rules)
                    {
                        createExpression(mode, rule.Formula);
                    }
                    break;
                case Query q:
                    createExpression(mode, q.Condition);
                    break;
                case LogicalExpression le:
                    le._expression = createExpression(mode, le.ExpressionValue, le.ExpressionType, smeValue);
                    return le._expression;
                default:
                    break;
            }
        }
        else
        {
            var op = "";
            switch (type)
            {
                case "$or":
                    op = "||";
                    break;
                case "$and":
                    op = "&&";
                    break;
                case "$match":
                    op = "$match";
                    break;
                case "$not":
                    op = "!";
                    break;
                case "$eq":
                    op = "==";
                    break;
                case "$ne":
                    op = "!=";
                    break;
                case "$gt":
                    op = ">";
                    break;
                case "$ge":
                    op = ">=";
                    break;
                case "$lt":
                    op = "<";
                    break;
                case "$le":
                    op = "<=";
                    break;
                case "$starts-with":
                    op = "StartsWith";
                    break;
                case "$ends-with":
                    op = "EndsWith";
                    break;
                case "$contains":
                    op = "Contains";
                    break;
            }

            List<LogicalExpression>? eList = null;
            if (obj is List<LogicalExpression>)
            {
                eList = (List<LogicalExpression>)obj;
            }

            switch (type)
            {
                case "$not":
                    if (eList?.Count == 1)
                    {
                        string value = createExpression(mode, eList[0]);
                        if (value != "$SKIP")
                        {
                            return "!" + value;
                        }
                        else
                        {
                            return "$SKIP";
                        }
                    }
                    break;
                case "$match":
                case "$and":
                case "$or":
                    if (eList?.Count > 0)
                    {
                        var par = new string[eList.Count];
                        par[0] = createExpression(mode, eList[0]);
                        if (eList.Count == 1)
                        {
                            if (op != "$match")
                            {
                                return par[0];
                            }
                            else
                            {
                                if (par[0] != null && par[0] != "$SKIP")
                                {
                                    return "$$match$$" + par[0] + "$$match$$";
                                }
                                return par[0];
                            }
                        }
                        else
                        {
                            int skipCount = 0;
                            if (par[0] == "$SKIP")
                            {
                                skipCount++;
                            }
                            for (int i = 1; i < eList.Count; i++)
                            {
                                par[i] = createExpression(mode, eList[i]);
                                if (par[i] == "$SKIP")
                                {
                                    skipCount++;
                                }
                            }
                            if (skipCount == eList.Count)
                            {
                                return "$SKIP";
                            }
                            if (skipCount != 0)
                            {
                                if (type == "$or")
                                {
                                    return "true";
                                }
                            }
                            if (op != "$match")
                            {
                                var result = "";
                                int count = 0;
                                for (int i = 0; i < eList.Count; i++)
                                {
                                    if (par[i] != null && par[i] != "$SKIP")
                                    {
                                        count++;
                                        if (result == "")
                                        {
                                            result = par[i];
                                        }
                                        else
                                        {
                                            result += " " + op + " " + par[i];
                                        }
                                    }
                                }
                                return "(" + result + ")"; // + "[" + skipCount + "/" + eList.Count + "]";
                            }
                            else
                            {
                                var result = "$$match$$";
                                int count = 0;
                                for (int i = 0; i < eList.Count; i++)
                                {
                                    if (par[i] != null && par[i] != "$SKIP")
                                    {
                                        count++;
                                        if (result == "")
                                        {
                                            result += par[i];
                                        }
                                        else
                                        {
                                            result += "$$" + par[i];
                                        }
                                    }
                                }
                                return result + "$$match$$";
                            }
                        }
                    }
                    break;
                case "$eq":
                case "$ne":
                case "$gt":
                case "$ge":
                case "$lt":
                case "$le":
                    if (eList != null)
                    {
                        if (eList[0].ExpressionType == "$numVal" || eList[1].ExpressionType == "$numVal")
                        {
                            smeValue = "mvalue";
                        }
                        else if (eList[0].ExpressionType == "$strVal" || eList[1].ExpressionType == "$strVal")
                        {
                            smeValue = "svalue";
                        }
                        string left = createExpression(mode, eList[0], smeValue: smeValue);
                        string right = createExpression(mode, eList[1], smeValue: smeValue);
                        if (left != "$SKIP" && right != "$SKIP")
                        {
                            if (right.StartsWith("$$tag$$"))
                            {
                                return "$ERROR";
                            }
                            if (left.StartsWith("$$tag$$"))
                            {
                                return $"{left} {op} {right}$$";
                            }

                            return "(" + left + " " + op + " " + right + ")";
                        }
                        else
                        {
                            return "$SKIP";
                        }
                    }
                    break;
                case "$starts-with":
                case "$ends-with":
                case "$contains":
                    if (eList != null)
                    {
                        var left = createExpression(mode, eList[0], smeValue: "svalue");
                        var right = createExpression(mode, eList[1], smeValue: "svalue");
                        if (left != "$SKIP" && right != "$SKIP")
                        {
                            if (right.StartsWith("$$tag$$"))
                            {
                                return "$ERROR";
                            }
                            if (left.StartsWith("$$tag$$"))
                            {
                                return $"{left}.{op}({right})$$";
                            }

                            return left + "." + op + "(" + right + ")";
                        }
                        else
                        {
                            return "$SKIP";
                        }
                    }
                    break;
                case "$boolean":
                    if (obj is bool b)
                    {
                        return b.ToString();
                    }
                    break;
                case "$attribute":
                    if (obj is LogicalExpression le)
                    {
                        return le.ExpressionType + "(" + le.ExpressionValue + ")";
                    }
                    break;
                case "$field":
                    if (obj is string)
                    {
                        var value = "" + (obj as string);

                        if (value.Contains("$sme."))
                        {
                            var tag = "path";
                            /*
                            if (value.Contains("[]"))
                            {
                                tag = "match";
                            }
                            */
                            // $sme with idShortPath
                            value = value.Replace("$sme.", "");
                            var split = value.Split("#");
                            value = ReplaceField(mode, $"$sme#{split[1]}", smeValue);
                            if (value == "$SKIP" || value == "$ERROR")
                            {
                                return value;
                            }

                            return $"$$tag$${tag}$${split[0]}$${value}$$";
                        }

                        return ReplaceField(mode, value, smeValue);
                    }
                    return "$ERROR";
                case "$strVal":
                    if (mode == "mvalue")
                    {
                        return "$SKIP";
                    }
                    if (obj is string)
                    {
                        var v = obj as string;
                        if (v == "$null")
                        {
                            return "null";
                        }
                        else
                        {
                            return "\"" + v + "\"";
                        }
                    }
                    break;
                case "$numVal":
                    if (mode == "svalue")
                    {
                        return "$SKIP";
                    }
                    if (obj is int or long or double)
                    {
                        return obj.ToString();
                    }
                    break;
                default:
                    break;
            }
        }
        return "$ERROR";
    }

    public static string ReplaceField(string mode, string value, string smeValue)
    {
        value = value.Replace("$aas#", "aas.");
        value = value.Replace("$sm#", "sm.");
        value = value.Replace("$sme#", "sme.");
        value = value.Replace("sm.Id", "sm.Identifier");
        switch (mode)
        {
            case "all":
            case "all-aas":
                if (smeValue == "svalue")
                {
                    if (value == "sme.value")
                    {
                        value = "svalue";
                    }
                }
                if (smeValue == "mvalue")
                {
                    if (value == "sme.value")
                    {
                        value = "mvalue";
                    }
                }
                if (mode == "all" && value.StartsWith("aas."))
                {
                    value = "$SKIP";
                }
                break;
            case "sm.":
                if (!value.StartsWith("sm."))
                {
                    value = "$SKIP";
                }
                break;
            case "aas.":
                if (!value.StartsWith("aas."))
                {
                    value = "$SKIP";
                }
                break;
            case "sme.":
                if (value != "sme.value" && value.StartsWith("sme."))
                {
                    value = value;
                }
                else
                {
                    value = "$SKIP";
                }
                break;
            case "svalue":
                if (value == "sme.value")
                {
                    value = "svalue";
                }
                else
                {
                    value = "$SKIP";
                }
                break;
            case "mvalue":
                if (value == "sme.value")
                {
                    value = "mvalue";
                }
                else
                {
                    value = "$SKIP";
                }
                break;
            default:
                value = "$ERROR";
                break;
        }
        return value;
    }

    public static AllAccessPermissionRules _accessRules = null;
    public static new List<Dictionary<string, string>> allAccessRuleExpressions = [];
    public static new Dictionary<string, string> accessRuleExpression = [];
    // public static string accessRuleExpression = "((sm.idShort==\"Nameplate\")||(sm.idShort==\"TechnicalData\"))";
    // public static string accessRuleExpression = "(sm.idShort==\"Nameplate\")";
    public void ParseAccessRules(string expression)
    {
        // mySecurityRules.ClearSecurityRules();
        allAccessRuleExpressions = new List<Dictionary<string, string>>();
        accessRuleExpression = new Dictionary<string, string>();
        Root deserializedData = null;

        var jsonSchema = "";
        if (System.IO.File.Exists("jsonschema-access.txt"))
        {
            jsonSchema = System.IO.File.ReadAllText("jsonschema-access.txt");
            string jsonData = expression;

            /*
            // Working, but AGPL 3.0
            // If needed for testing, include again
            // Newtonsoft
            // Schema parsen
            JSchema schema = JSchema.Parse(jsonSchema);

            // JSON-Daten parsen
            JObject jsonObject = JObject.Parse(jsonData);

            // Validierung durchfÃ¼hren
            IList<string> validationErrors = new List<string>();
            bool isValid = jsonObject.IsValid(schema, out validationErrors);
            */
            /*
            // Not working
            // NJsonSchema;
            // Schema parsen
            JsonSchema schema = JsonSchema.FromJsonAsync(jsonSchema).Result;

            // JSON-Daten parsen
            JObject jsonObject = JObject.Parse(jsonData);

            // Validierung durchfÃ¼hren
            ICollection<ValidationError> validationErrors = schema.Validate(jsonObject);
            bool isValid = validationErrors.Count == 0;
            */

            /*
            // Does not work
            // Json.Schema;
            var schema = JsonSchema.FromText(jsonSchema);
            var result = schema.Evaluate(JsonNode.Parse(jsonData));
            bool isValid = result.IsValid;
            var validationErrors = result.Errors;
            if (!isValid)
            {
                foreach (var error in validationResults.Errors)
                {
                    validationErrors.Add(error.ToString());
                }
            }
            */

            // no schema checking currently, only checking by json grammar
            var isValid = true;

            if (isValid)
            {
                Console.WriteLine("JSON is valid.");
                try
                {
                    deserializedData = JsonConvert.DeserializeObject<Root>(jsonData);
                    if (deserializedData != null)
                    {
                        Console.WriteLine("Successfully deserialized.");
                    }
                    else
                    {
                        isValid = false;
                    }
                }
                catch
                {
                    isValid = false;
                }
            }
            if (!isValid)
            {
                Console.WriteLine("âŒ JSON not valid:");
                /*
                foreach (var error in validationErrors)
                {
                    // Console.WriteLine($"- {error}");
                    Console.WriteLine(error.Key + ": " + error.Value);
                }
                */
                return;
            }
        }
        else
        {
            Console.WriteLine("âŒ jsonschema-access.txt not found.");
            return;
        }

        var allRules = deserializedData?.AllAccessPermissionRules;
        if (deserializedData == null || allRules == null)
        {
            return;
        }

        _accessRules = allRules;
        foreach (var rule in allRules.Rules)
        {
            // mode: all, sm., sme., svalue, mvalue
            List<LogicalExpression?> logicalExpressions = [];
            List<Dictionary<string, string>> conditions = [];
            logicalExpressions.Add(rule.Formula);
            conditions.Add(rule._formula_conditions);
            if (rule.Filter != null)
            {
                logicalExpressions.Add(rule.Filter.Condition);
                conditions.Add(rule._filter_conditions);
            }
            if (logicalExpressions.Count != 0)
            {
                for (var i = 0; i < logicalExpressions.Count; i++)
                {
                    var le = logicalExpressions[i];
                    if (le != null)
                    {
                        createExpression("all", le);
                        conditions[i].Add("all", le._expression);
                        createExpression("sm.", le);
                        if (le._expression == "$SKIP")
                        {
                            le._expression = "";
                        }
                        else
                        {
                            le._expression = le._expression.Replace("sm.", "");
                        }
                        conditions[i].Add("sm.", le._expression);
                        createExpression("sme.", le);
                        if (le._expression == "$SKIP")
                        {
                            le._expression = "";
                        }
                        else
                        {
                            le._expression = le._expression.Replace("sme.", "");
                        }
                        conditions[i].Add("sme.", le._expression);
                        createExpression("svalue", le);
                        if (le._expression == "$SKIP")
                        {
                            le._expression = "";
                        }
                        le._expression = le._expression.Replace("svalue", "Value");
                        conditions[i].Add("svalue", le._expression);
                        createExpression("mvalue", le);
                        if (le._expression == "$SKIP")
                        {
                            le._expression = "";
                        }
                        le._expression = le._expression.Replace("mvalue", "Value");
                        conditions[i].Add("mvalue", le._expression);
                    }
                }
            }

            var accessRuleExpression = new Dictionary<string, string>();
            ParseAccessRule(rule, accessRuleExpression);
            allAccessRuleExpressions.Add(accessRuleExpression);
        }
    }

    List<string> Names = new List<string>();
    string access = "";
    string rights = "";
    string global = "";
    bool isClaim = false;
    string claim = "";
    bool filter = false;
    bool route = false;
    void ParseAccessRule(AccessPermissionRule rule, Dictionary<string, string> accessRuleExpression)
    {
        foreach (var c in rule._formula_conditions)
        {
            accessRuleExpression.Add(c.Key, c.Value);
        }
        if (rule._filter_conditions != null && rule._filter_conditions.Count != 0)
        {
            accessRuleExpression["filter"] = rule._filter_conditions["all"];
        }
        var attributes = rule.Acl?.Attributes;
        if (attributes != null && attributes.Count != 0)
        {
            foreach (var a in attributes)
            {
                if (a.ItemType == "CLAIM")
                {
                    claim = a.Value;
                    accessRuleExpression["claim"] = claim;
                }
            }
        }
        var routes = rule.Objects?.Where(o => o.ItemType == "ROUTE").ToList();
        var rightList = rule.Acl?.Rights?.ToList();
        rights = "";
        if (rightList != null && rightList.Count != 0)
        {
            for (var i = 0; i < rightList.Count; i++)
            {
                if (i == 0)
                {
                    rights = rightList[i];
                }
                else
                {
                    rights += " " + rightList[i];
                }
                if (routes != null && routes.Count != 0)
                {
                    foreach (var r in routes)
                    {
                        mySecurityRules.AddSecurityRule(claim, "ALLOW", rightList[i], "api", "", r.Value);
                    }
                }
            }
        }
        accessRuleExpression["right"] = rights;
    }
}

