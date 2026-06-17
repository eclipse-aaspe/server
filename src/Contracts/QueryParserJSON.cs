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

using System.Collections.Generic;
// using Newtonsoft.Json.Schema;
// using NJsonSchema;
// using NJsonSchema.Validation;
// using Json.Schema;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AasSecurity.Models;
using Contracts;
using Irony.Parsing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using static System.Net.Mime.MediaTypeNames;

public class QueryGrammarJSON : Grammar
{
    /// <summary>SQL boolean literals for constant folding ($boolean → overall / scope SQL).</summary>
    private const string SqlBoolTrue = "(1=1)";
    private const string SqlBoolFalse = "(0=1)";

    /// <summary>While evaluating the RHS of a comparison in <c>value</c> mode, indicates Annotation is valueType vs language.</summary>
    private static readonly Stack<SmeSemanticKind> _pendingValueStrSemantic = new();

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

    public static string optimizeTrueFalse(string expression)
    {
        while (
            expression.Contains("(True)") ||
            expression.Contains("True || True") ||
            expression.Contains("True && True") ||
            expression.Contains("(False)") ||
            expression.Contains("False || False") ||
            expression.Contains("False && False") ||
            expression.Contains("True || False") ||
            expression.Contains("True && False") ||
            expression.Contains("False || True") ||
            expression.Contains("False && True")
            )
        {
            expression = expression.Replace("(True)", "True");
            expression = expression.Replace("True || True", "True");
            expression = expression.Replace("True && True", "True");
            expression = expression.Replace("(False)", "False");
            expression = expression.Replace("False || False", "False");
            expression = expression.Replace("False && False", "False");
            expression = expression.Replace("True || False", "True");
            expression = expression.Replace("True && False", "False");
            expression = expression.Replace("False || True", "True");
            expression = expression.Replace("False && True", "False");
        }
        return expression;
    }

    private static bool IsSmeValueTypeField(string fv)
    {
        if (string.IsNullOrEmpty(fv))
            return false;
        if (fv.Contains("$sme.", StringComparison.Ordinal))
        {
            var without = fv.Replace("$sme.", "", StringComparison.Ordinal);
            var parts = without.Split('#');
            return parts.Length >= 2 && parts[1] == "valueType";
        }
        return fv.Replace("$sme#", "sme.", StringComparison.Ordinal) == "sme.valueType";
    }

    private static bool IsSmeLanguageField(string fv)
    {
        if (string.IsNullOrEmpty(fv))
            return false;
        if (fv.Contains("$sme.", StringComparison.Ordinal))
        {
            var without = fv.Replace("$sme.", "", StringComparison.Ordinal);
            var parts = without.Split('#');
            return parts.Length >= 2 && parts[1] == "language";
        }
        return fv.Replace("$sme#", "sme.", StringComparison.Ordinal) == "sme.language";
    }

    private static SmeSemanticKind GetSemanticKindFromFieldExpr(LogicalExpression? e)
    {
        if (e?.ExpressionType != "$field" || e.ExpressionValue is not string s)
            return SmeSemanticKind.None;
        if (IsSmeValueTypeField(s))
            return SmeSemanticKind.ValueType;
        if (IsSmeLanguageField(s))
            return SmeSemanticKind.Language;
        return SmeSemanticKind.None;
    }

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
                    return createExpression(mode, le.ExpressionValue, le.ExpressionType, smeValue);
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
                                        if (type == "$or")
                                        {
                                            if (par[i] == "True")
                                            {
                                                return "True";
                                            }
                                        }
                                        if (type == "$and")
                                        {
                                            if (par[i] == "False")
                                            {
                                                return "False";
                                            }
                                        }
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
                        if (eList[0].ExpressionType == "$strVal" || eList[1].ExpressionType == "$strVal")
                        {
                            smeValue = "svalue";
                        }
                        else if (eList[0].ExpressionType == "$numVal" || eList[1].ExpressionType == "$numVal")
                        {
                            smeValue = "mvalue";
                        }
                        else if (eList[0].ExpressionType == "$dateTimeVal" || eList[1].ExpressionType == "$dateTimeVal")
                        {
                            smeValue = "dtvalue";
                        }
                        else if (eList[0].ExpressionType == "$hexVal" || eList[1].ExpressionType == "$hexVal")
                        {
                            smeValue = "mvalue";
                        }
                        var sem = GetSemanticKindFromFieldExpr(eList[0]);
                        string left = createExpression(mode, eList[0], smeValue: smeValue);
                        if (mode == "value" && sem != SmeSemanticKind.None)
                            _pendingValueStrSemantic.Push(sem);
                        string right;
                        try
                        {
                            right = createExpression(mode, eList[1], smeValue: smeValue);
                        }
                        finally
                        {
                            if (mode == "value" && sem != SmeSemanticKind.None)
                                _pendingValueStrSemantic.Pop();
                        }

                        // Platzhalter / vtvalue-langvalue-Auflösung erst nach $SKIP: sonst wird z. B. mit "$SKIP" unquotet.
                        if (left == "$SKIP" || right == "$SKIP")
                            return "$SKIP";

                        if (TryResolveSmeValueTypeLanguage(mode, type, left, right, out var resolvedVtLang))
                            return resolvedVtLang;

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
                    break;
                case "$starts-with":
                case "$ends-with":
                case "$contains":
                    if (eList != null)
                    {
                        var left = createExpression(mode, eList[0], smeValue: "svalue");
                        var right = createExpression(mode, eList[1], smeValue: "svalue");
                        if (left == "$SKIP" || right == "$SKIP")
                            return "$SKIP";

                        if (TryResolveSmeValueTypeLanguage(mode, type, left, right, out var resolvedVtLangStr))
                            return resolvedVtLangStr;

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
                        if (mode == "value" && _pendingValueStrSemantic.Count > 0)
                        {
                            var k = _pendingValueStrSemantic.Peek();
                            if (k == SmeSemanticKind.ValueType)
                            {
                                if (!SmeQueryPrefilter.TrySerializeDataTypeAnnotation(v, out var ann))
                                    return "$ERROR";
                                var esc = SmeQueryPrefilter.EscapeForDynamicLinq(ann);
                                return "\"" + esc + "\"";
                            }
                            if (k == SmeSemanticKind.Language)
                            {
                                if (!LanguageQueryPrefilter.TryValidateLanguageLiteral(v, out var lang))
                                    return "$ERROR";
                                var esc = SmeQueryPrefilter.EscapeForDynamicLinq(lang);
                                return "\"" + esc + "\"";
                            }
                        }
                        return "\"" + v + "\"";
                    }
                    break;
                case "$numVal":
                    if (mode == "svalue")
                    {
                        return "$SKIP";
                    }
                    if (obj is int or long or double)
                    {
                        return Convert.ToString(obj, CultureInfo.InvariantCulture);
                    }
                    break;
                case "$dateTimeVal":
                    if (mode == "svalue")
                    {
                        return "$SKIP";
                    }
                    if (obj is string)
                    {
                        var v = obj as string;
                        return "\"" + v + "\"";
                    }
                    break;
                case "$hexVal":
                    if (mode == "svalue")
                    {
                        return "$SKIP";
                    }
                    if (obj is string)
                    {
                        var v = obj as string;
                        v = v.Replace("16#", "");
                        var value = Convert.ToInt64(v, 16);
                        return value.ToString();
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
        // Semantic SME fields: map before $aas# / $sm# / $sme# rewrites (same layer as sme.value in the switch below).
        var vNorm = value.Contains("$sme#", StringComparison.Ordinal) ? value.Replace("$sme#", "sme.") : value;
        if (vNorm == "sme.valueType")
        {
            if (mode == "all" || mode == "all-aas" || mode == "sme.")
                return "vtvalue";
            if (mode == "value")
                return "Annotation";
        }
        else if (vNorm == "sme.language")
        {
            if (mode == "all" || mode == "all-aas" || mode == "sme.")
                return "langvalue";
            if (mode == "value")
                return "Annotation";
        }

        if (value == "$aas#id")
        {
            value = "$aas#identifier";
        }
        if (value == "$sm#id")
        {
            value = "$sm#identifier";
        }

        value = value.Replace("$aas#", "aas.");
        value = value.Replace("$sm#", "sm.");
        value = value.Replace("$sme#", "sme.");

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
                if (smeValue == "dtvalue")
                {
                    if (value == "sme.value")
                    {
                        value = "dtvalue";
                    }
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
            case "value":
                if (value == "sme.value")
                {
                    value = smeValue;
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

    private static bool TryUnquoteDynamicStrVal(string quoted, out string inner)
    {
        inner = "";
        quoted = quoted.Trim();
        if (quoted.Length < 2 || !quoted.StartsWith('"') || !quoted.EndsWith('"'))
            return false;
        inner = Regex.Unescape(quoted.Substring(1, quoted.Length - 2));
        return true;
    }

    /// <summary>
    /// Resolves SME valueType/language field placeholders using
    /// grammar <paramref name="queryType"/> (e.g. <c>$eq</c>, <c>$contains</c>), not Dynamic LINQ operators.
    /// </summary>
    private static bool TryResolveSmeValueTypeLanguage(
        string mode, string queryType, string left, string right, out string result)
    {
        result = "";
        switch (queryType)
        {
            case "$eq":
            case "$ne":
                if (!TryUnquoteDynamicStrVal(right, out var strLit))
                    return false;
                return TryResolveSmeVtLangComparison(mode, queryType, left, strLit, out result);

            case "$contains":
            case "$starts-with":
            case "$ends-with":
                if (!TryUnquoteDynamicStrVal(right, out var needle))
                    return false;
                return TryResolveSmeVtLangStringMethod(mode, queryType, left, needle, out result);

            default:
                return false;
        }
    }

    private static bool TryResolveSmeVtLangComparison(
        string mode, string queryType, string left, string strLit, out string result)
    {
        result = "";
        var isEq = queryType == "$eq";

        if (left == "vtvalue")
        {
            if (mode == "all" || mode == "all-aas")
            {
                if (!SmeQueryPrefilter.TryGetTValueForValueTypeLiteral(strLit, out var disc))
                {
                    result = "$ERROR";
                    return true;
                }
                if (!SmeQueryPrefilter.TrySerializeDataTypeAnnotation(strLit, out var ann))
                {
                    result = "$ERROR";
                    return true;
                }
                var esc = SmeQueryPrefilter.EscapeForDynamicLinq(ann);
                result = isEq
                    ? $"(sme.TValue == \"{disc}\" && valueAnnotation == \"{esc}\")"
                    : $"(sme.TValue != \"{disc}\" || valueAnnotation != \"{esc}\")";
                return true;
            }
            if (mode == "sme.")
            {
                if (!SmeQueryPrefilter.TryGetTValueForValueTypeLiteral(strLit, out var disc))
                {
                    result = "$ERROR";
                    return true;
                }
                result = isEq ? $"(TValue == \"{disc}\")" : $"(TValue != \"{disc}\")";
                return true;
            }
            return false;
        }

        if (left == "langvalue")
        {
            if (mode == "all" || mode == "all-aas")
            {
                if (!LanguageQueryPrefilter.TryValidateLanguageLiteral(strLit, out var lang))
                {
                    result = "$ERROR";
                    return true;
                }
                var esc = SmeQueryPrefilter.EscapeForDynamicLinq(lang);
                result = isEq
                    ? $"(sme.TValue == \"S\" && valueAnnotation == \"{esc}\")"
                    : $"(sme.TValue != \"S\" || valueAnnotation != \"{esc}\")";
                return true;
            }
            if (mode == "sme.")
            {
                result = isEq ? "(TValue == \"S\")" : "(TValue != \"S\")";
                return true;
            }
            return false;
        }

        return false;
    }

    private static bool TryResolveSmeVtLangStringMethod(
        string mode, string queryType, string left, string needle, out string result)
    {
        result = "";
        var esc = SmeQueryPrefilter.EscapeForDynamicLinq(needle);

        if (left == "vtvalue")
        {
            result = "$ERROR";
            return true;
        }

        if (left == "langvalue")
        {
            if (mode == "all" || mode == "all-aas")
            {
                result = queryType switch
                {
                    "$contains" => $"(sme.TValue == \"S\" && valueAnnotation.Contains(\"{esc}\"))",
                    "$starts-with" => $"(sme.TValue == \"S\" && valueAnnotation.StartsWith(\"{esc}\"))",
                    "$ends-with" => $"(sme.TValue == \"S\" && valueAnnotation.EndsWith(\"{esc}\"))",
                    _ => ""
                };
                return result != "";
            }
            if (mode == "sme.")
            {
                result = "(TValue == \"S\")";
                return true;
            }
            return false;
        }

        return false;
    }

    public static AllAccessPermissionRules _accessRules = null;
    public static SqlConditions? allAccessRuleSqlConditions = null;
    public void ParseAccessRules(string expression)
    {
        // mySecurityRules.ClearSecurityRules();
        allAccessRuleSqlConditions = null;
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
                Console.WriteLine("JSON not valid:");
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
            Console.WriteLine("jsonschema-access.txt not found.");
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
            List<LogicalExpression?> logicalExpressions = [];
            logicalExpressions.Add(rule.Formula);
            if (rule.Filter != null)
            {
                logicalExpressions.Add(rule.Filter.Condition);
            }
            if (logicalExpressions.Count != 0)
            {
                for (var i = 0; i < logicalExpressions.Count; i++)
                {
                    var le = logicalExpressions[i];
                    if (le != null)
                    {
                        var sc = CreateSqlConditions(le);
                        if (i == 0)
                            rule._formula_sqlConditions = sc;
                        else
                            rule._filter_sqlConditions = sc;

                    }
                }
            }

            RegisterAccessRuleRoutes(rule);

            // Accumulate SQL access conditions across all rules as OR(rule FORMULA AND rule FILTER).
            var ruleSqlConditions = SqlConditionsMerger.Merge(rule._formula_sqlConditions, rule._filter_sqlConditions);
            if (ruleSqlConditions != null)
            {
                allAccessRuleSqlConditions = allAccessRuleSqlConditions == null
                    ? ruleSqlConditions.Clone()
                    : SqlConditionsMerger.OrMerge(allAccessRuleSqlConditions, ruleSqlConditions);
            }
        }
    }

    string rights = "";
    string claim = "";
    void RegisterAccessRuleRoutes(AccessPermissionRule rule)
    {
        var attributes = rule.Acl?.Attributes;
        if (attributes != null && attributes.Count != 0)
        {
            foreach (var a in attributes)
            {
                if (a.ItemType == "CLAIM")
                {
                    claim = a.Value;
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
    }

    // -------------------------------------------------------------------------
    // Direct SQL generation — replaces the LINQ-string conditionsExpression path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Traverses the AST rooted at <paramref name="le"/> and produces a <see cref="SqlConditions"/>
    /// that <c>CombineTablesLEFT</c> can assemble into raw SQL without any EF Core / LINQ involvement.
    /// </summary>
    public static SqlConditions CreateSqlConditions(LogicalExpression le)
    {
        var sc = new SqlConditions();
        var ctx = new SqlBuildContext(sc);

        // Scope filters — one pass per scope (mirrors createExpression modes)
        sc.FormulaConditions["aas"]   = BuildScopeSql(le, SqlScope.Aas)   ?? "";
        sc.FormulaConditions["sm"]    = BuildScopeSql(le, SqlScope.Sm)     ?? "";
        sc.FormulaConditions["sme"]   = BuildScopeSql(le, SqlScope.Sme)    ?? "";
        sc.FormulaConditions["value"] = BuildScopeSql(le, SqlScope.Value)  ?? "";

        // Overall condition with path/match placeholders
        sc.FormulaConditions["all"] = BuildOverallSql(le, ctx);

        return sc;
    }

    // ------------------------------------------------------------------
    // Internal context threaded through the recursive build
    // ------------------------------------------------------------------
    private class SqlBuildContext(SqlConditions sc)
    {
        public SqlConditions Sc { get; } = sc;
        public int PathIndex { get; set; } = 0;
        public int MatchIndex { get; set; } = 0;
        public int ExistsIndex { get; set; } = 0;
    }

    private enum SqlScope { Aas, Sm, Sme, Value }

    // ------------------------------------------------------------------
    // Scope-filter builder (produces a WHERE predicate for one DbSet)
    // ------------------------------------------------------------------
    private static string? BuildScopeSql(LogicalExpression le, SqlScope scope)
    {
        var result = BuildScopeSqlNode(le.ExpressionValue, le.ExpressionType, scope, "");
        if (result == "$SKIP" || result == "$ERROR" || result == null)
            return "";
        return result == "true" || result == SqlBoolTrue ? "" : result;
    }

    private static string? BuildScopeSqlNode(object? obj, string type, SqlScope scope, string smeValue)
    {
        if (obj == null) return "$SKIP";

        if (obj is bool boolLit)
            return boolLit ? SqlBoolTrue : SqlBoolFalse;

        // Recurse through LogicalExpression wrapper
        if (obj is LogicalExpression le)
            return BuildScopeSqlNode(le.ExpressionValue, le.ExpressionType, scope, smeValue);

        if (obj is List<LogicalExpression> eList)
        {
            switch (type)
            {
                case "$not":
                    if (eList.Count == 1)
                    {
                        var inner = BuildScopeSqlNode(eList[0], scope, smeValue);
                        return inner == "$SKIP" ? "$SKIP" : inner == null ? null : $"NOT ({inner})";
                    }
                    return "$SKIP";

                case "$and":
                {
                    var parts = eList
                        .Select(e => BuildScopeSqlNode(e, scope, smeValue))
                        .Where(s => s != null && s != "$SKIP" && s != "$ERROR")
                        .ToList();
                    if (parts.Count == 0) return "$SKIP";
                    if (parts.Any(p => p == "$ERROR")) return "$ERROR";
                    parts = FoldBooleanAndParts(parts);
                    if (parts.Count == 1)
                        return parts[0];
                    // Value-scope → inner ValueSets Vorfilter only: never AND (one row cannot combine arbitrary
                    // leaves); outer WHERE + path joins keep full semantics.
                    var joiner = scope == SqlScope.Value ? " OR " : " AND ";
                    return "(" + string.Join(joiner, parts) + ")";
                }

                case "$or":
                {
                    // Most scope prefilters must stay conservative across OR:
                    // if any satisfiable branch has no predicate for that scope, that scope cannot be restricted.
                    // Example: (aas=A) OR (aas=B) OR (sm=TechnicalData AND sme.value=X) must yield no AAS
                    // prefilter; otherwise the third branch is filtered out before the overall condition can match.
                    var branchResults = eList
                        .Select(e => BuildScopeSqlNode(e, scope, smeValue))
                        .ToList();
                    if (branchResults.Any(p => p == "$ERROR")) return "$ERROR";
                    if (branchResults.Any(p => p == "$SKIP" || p == null || p == SqlBoolTrue))
                        return SqlBoolTrue;
                    var parts = branchResults
                        .Where(p => p != null && p != "$SKIP" && p != SqlBoolTrue && p != SqlBoolFalse)
                        .Select(p => p!)
                        .ToList();
                    if (parts.Count == 0)
                        return branchResults.All(p => p == SqlBoolFalse) ? SqlBoolFalse : "$SKIP";
                    parts = FoldBooleanOrParts(parts);
                    if (parts.Count == 1)
                        return parts[0];
                    return "(" + string.Join(" OR ", parts) + ")";
                }

                case "$match":
                    // $match conditions only appear in the overall condition, not in scope filters
                    return "$SKIP";

                case "$eq": case "$ne": case "$gt": case "$ge": case "$lt": case "$le":
                case "$starts-with": case "$ends-with": case "$contains":
                    return BuildScopeComparisonSql(eList, type, scope);
            }
        }

        return "$SKIP";
    }

    private static string? BuildScopeSqlNode(LogicalExpression le, SqlScope scope, string smeValue)
        => BuildScopeSqlNode(le.ExpressionValue, le.ExpressionType, scope, smeValue);

    private static string? BuildScopeComparisonSql(List<LogicalExpression> eList, string type, SqlScope scope)
    {
        if (eList.Count < 2) return "$SKIP";
        var leftNode  = eList[0];
        var rightNode = eList[1];

        // Determine smeValue context from rhs type
        var smeValue = leftNode.ExpressionType == "$strVal" || rightNode.ExpressionType == "$strVal" ? "svalue"
                     : leftNode.ExpressionType == "$numVal" || rightNode.ExpressionType == "$numVal" ? "mvalue"
                     : leftNode.ExpressionType == "$dateTimeVal" || rightNode.ExpressionType == "$dateTimeVal" ? "dtvalue"
                     : leftNode.ExpressionType == "$hexVal" || rightNode.ExpressionType == "$hexVal" ? "mvalue"
                     : "";

        if (leftNode.ExpressionType == "$attribute" || rightNode.ExpressionType == "$attribute")
        {
            if (scope != SqlScope.Sm)
                return "$SKIP";

            var leftClaimSql = BuildClaimOrLiteralSql(leftNode, smeValue);
            if (leftClaimSql == "$SKIP" || leftClaimSql == null) return "$SKIP";
            var rightClaimSql = BuildClaimOrLiteralSql(rightNode, smeValue);
            if (rightClaimSql == "$SKIP" || rightClaimSql == null) return "$SKIP";
            return CombineComparisonSql(leftClaimSql, type, rightClaimSql);
        }

        var leftSql  = BuildScopeFieldSql(leftNode, scope, smeValue);
        if (leftSql == "$SKIP" || leftSql == null) return "$SKIP";

        // Path conditions ($sme.idShortPath) are not scope filters
        if (leftSql.StartsWith("$$path$$")) return "$SKIP";

        var rightSql = BuildScopeLiteralSql(rightNode, smeValue);
        if (rightSql == "$SKIP" || rightSql == null) return "$SKIP";

        return CombineComparisonSql(leftSql, type, rightSql);
    }

    /// <summary>Maps a $field node to a SQL column name for the given scope; returns $SKIP if not relevant.</summary>
    private static string? BuildScopeFieldSql(LogicalExpression node, SqlScope scope, string smeValue)
    {
        if (node.ExpressionType != "$field" || node.ExpressionValue is not string raw)
            return "$SKIP";

        // "$sme.{idShortPath}#field" — overall uses path joins; for scope SQL we emit the same SME
        // columns as "$sme#field" so FormulaConditions["sme"] can Vorfilter the SMESets subquery in
        // BuildRawSql (inner filter may be slightly wider than path-specific overall; outer WHERE unchanged).
        var smeDotIdx = raw.IndexOf("$sme.", StringComparison.OrdinalIgnoreCase);
        if (smeDotIdx >= 0 && raw.Contains('#'))
        {
            var withoutPrefix = raw[(smeDotIdx + "$sme.".Length)..];
            var hashIdx = withoutPrefix.IndexOf('#');
            if (hashIdx > 0 && hashIdx < withoutPrefix.Length - 1)
            {
                var fieldName = withoutPrefix[(hashIdx + 1)..];
                if (fieldName == "value")
                {
                    if (scope != SqlScope.Value)
                        return "$SKIP";

                    // Path and match values are enforced by their generated subqueries. They must not widen the
                    // global ValueSets prefilter used for direct "$sme#value" overall references.
                    if (!string.IsNullOrEmpty(withoutPrefix[..hashIdx]))
                        return "$SKIP";

                    return smeValue switch
                    {
                        "svalue"  => "v.\"SValue\"",
                        "mvalue"  => "v.\"NValue\"",
                        "dtvalue" => "v.\"DTValue\"",
                        _ => "$SKIP"
                    };
                }
                if (scope == SqlScope.Sme && string.IsNullOrEmpty(withoutPrefix[..hashIdx]))
                {
                    var col = SmeColumnSql(fieldName);
                    if (col != null)
                        return col;
                }
            }
            return "$SKIP";
        }

        // Semantic fields: vtvalue / langvalue
        var vNorm = raw.StartsWith("$sme#") ? raw.Replace("$sme#", "sme.") : raw;
        if (vNorm == "$sme#valueType" || vNorm == "sme.valueType")
            return scope == SqlScope.Sme ? "\"TValue\"" : "$SKIP";
        if (vNorm == "$sme#language" || vNorm == "sme.language")
            return scope == SqlScope.Sme ? "\"TValue\"" : "$SKIP";

        // Normalize id aliases
        var field = raw == "$aas#id" ? "$aas#identifier"
                  : raw == "$sm#id"  ? "$sm#identifier"
                  : raw;

        if (field.StartsWith("$aas#"))
        {
            if (scope != SqlScope.Aas) return "$SKIP";
            var col = AasColumnSql(field["$aas#".Length..]);
            return col == null ? "$SKIP" : col;
        }
        if (field.StartsWith("$sm#"))
        {
            if (scope != SqlScope.Sm) return "$SKIP";
            var col = SmColumnSql(field["$sm#".Length..]);
            return col == null ? "$SKIP" : col;
        }
        if (field.StartsWith("$sme#"))
        {
            var prop = field["$sme#".Length..];
            if (prop == "value")
            {
                // sme.value is a value-table field — always qualify with v. for use in JOIN WHERE
                return scope switch
                {
                    SqlScope.Value => smeValue switch
                    {
                        "svalue"  => "v.\"SValue\"",
                        "mvalue"  => "v.\"NValue\"",
                        "dtvalue" => "v.\"DTValue\"",
                        _ => "$SKIP"
                    },
                    _ => "$SKIP"
                };
            }
            if (scope != SqlScope.Sme) return "$SKIP";
            return SmeColumnSql(prop);
        }
        return "$SKIP";
    }

    private static List<string> FoldBooleanAndParts(List<string> parts)
    {
        if (parts.Any(p => p == SqlBoolFalse))
            return [SqlBoolFalse];
        var rest = parts.Where(p => p != SqlBoolTrue).ToList();
        return rest.Count == 0 ? [SqlBoolTrue] : rest;
    }

    private static List<string> FoldBooleanOrParts(List<string> parts)
    {
        if (parts.Any(p => p == SqlBoolTrue))
            return [SqlBoolTrue];
        var rest = parts.Where(p => p != SqlBoolFalse).ToList();
        return rest.Count == 0 ? [SqlBoolFalse] : rest;
    }

    // ------------------------------------------------------------------
    // Overall-condition builder — produces SQL with path/match placeholders
    // ------------------------------------------------------------------
    private static string BuildOverallSql(LogicalExpression le, SqlBuildContext ctx)
    {
        var result = BuildOverallSqlNode(le.ExpressionValue, le.ExpressionType, ctx, "");
        if (result == "$SKIP" || result == "$ERROR" || result == null)
            return "";
        return result;
    }

    private static string? BuildOverallSqlNode(object? obj, string type, SqlBuildContext ctx, string smeValue)
    {
        if (obj == null) return "$SKIP";

        if (obj is bool boolLit)
            return boolLit ? SqlBoolTrue : SqlBoolFalse;

        if (obj is LogicalExpression le)
            return BuildOverallSqlNode(le.ExpressionValue, le.ExpressionType, ctx, smeValue);

        if (obj is List<LogicalExpression> eList)
        {
            switch (type)
            {
                case "$not":
                    if (eList.Count == 1)
                    {
                        var inner = BuildOverallSqlNode(eList[0], type: eList[0].ExpressionType, ctx, smeValue);
                        if (inner == "$SKIP" || inner == null) return "$SKIP";
                        return $"NOT ({inner})";
                    }
                    return "$SKIP";

                case "$and":
                {
                    if (TryBuildDirectValueExistsPredicate(eList, type, out var existsPredicate))
                    {
                        if (existsPredicate == SqlBoolTrue || existsPredicate == SqlBoolFalse)
                            return existsPredicate;
                        return AddExistsCondition(ctx, existsPredicate);
                    }

                    var parts = new List<string>();
                    var valueExistsPredicates = new List<string>();
                    foreach (var e in eList)
                    {
                        if (TryBuildDirectValueExistsPredicate(e, out var valueExistsPredicate))
                        {
                            if (valueExistsPredicate == SqlBoolFalse)
                                return SqlBoolFalse;
                            if (valueExistsPredicate != SqlBoolTrue)
                                valueExistsPredicates.Add(valueExistsPredicate);
                            continue;
                        }

                        var part = BuildOverallSqlNode(e.ExpressionValue, e.ExpressionType, ctx, smeValue);
                        if (part == "$ERROR")
                            return "$ERROR";
                        // Short-circuit: do not visit siblings (no path/match side effects) once AND is unsatisfiable.
                        if (part == SqlBoolFalse)
                            return SqlBoolFalse;
                        if (part != null && part != "$SKIP")
                            parts.Add(part);
                    }

                    if (valueExistsPredicates.Count > 0)
                    {
                        var valueExistsPredicate = valueExistsPredicates.Count == 1
                            ? valueExistsPredicates[0]
                            : "(" + string.Join(" AND ", valueExistsPredicates) + ")";
                        parts.Add(AddExistsCondition(ctx, valueExistsPredicate));
                    }

                    if (parts.Count == 0)
                        return "$SKIP";
                    parts = FoldBooleanAndParts(parts);
                    if (parts.Count == 1)
                        return parts[0];
                    return "(" + string.Join(" AND ", parts) + ")";
                }

                case "$or":
                {
                    if (TryBuildDirectValueExistsPredicate(eList, type, out var existsPredicate))
                    {
                        if (existsPredicate == SqlBoolTrue || existsPredicate == SqlBoolFalse)
                            return existsPredicate;
                        return AddExistsCondition(ctx, existsPredicate);
                    }

                    var parts = new List<string>();
                    foreach (var e in eList)
                    {
                        var part = BuildOverallSqlNode(e.ExpressionValue, e.ExpressionType, ctx, smeValue);
                        if (part == "$ERROR")
                            return "$ERROR";
                        // Short-circuit: later branches must not register paths/joins if expression is already tautological.
                        if (part == SqlBoolTrue)
                            return SqlBoolTrue;
                        if (part != null && part != "$SKIP")
                            parts.Add(part);
                    }

                    if (parts.Count == 0)
                        return "$SKIP";
                    parts = FoldBooleanOrParts(parts);
                    if (parts.Count == 1)
                        return parts[0];
                    return "(" + string.Join(" OR ", parts) + ")";
                }

                case "$match":
                {
                    var matchJoin = new MatchJoin { Placeholder = $"match{ctx.MatchIndex}" };
                    foreach (var e in eList)
                    {
                        var pj = TryBuildMatchPathJoin(e);
                        if (pj != null)
                        {
                            pj.Placeholder = $"path{ctx.PathIndex++}";
                            matchJoin.Paths.Add(pj);
                        }
                    }
                    if (matchJoin.Paths.Count == 0) return "$SKIP";

                    var joinParts = new List<string>();
                    for (int k = 1; k < matchJoin.Paths.Count; k++)
                        joinParts.Add($"Path1.SMId = Path{k + 1}.SMId");
                    matchJoin.JoinConditionSql = string.Join(" AND ", joinParts);

                    ctx.Sc.Matches.Add(matchJoin);
                    ctx.MatchIndex++;
                    return $"$${matchJoin.Placeholder}$$";
                }

                case "$eq": case "$ne": case "$gt": case "$ge": case "$lt": case "$le":
                case "$starts-with": case "$ends-with": case "$contains":
                    return BuildOverallComparisonSql(eList, type, ctx);
            }
        }

        return "$SKIP";
    }

    private static string AddExistsCondition(SqlBuildContext ctx, string predicateSql)
    {
        var idx = ctx.ExistsIndex++;
        var exists = new ExistsCondition
        {
            Placeholder = $"exists{idx}",
            PredicateSql = predicateSql
        };
        ctx.Sc.ExistsConditions.Add(exists);
        return $"$${exists.Placeholder}$$";
    }

    private static bool TryBuildDirectValueExistsPredicate(
        List<LogicalExpression> eList,
        string type,
        out string predicateSql)
    {
        predicateSql = "";

        if (type is "$and" or "$or")
        {
            var parts = new List<string>();
            foreach (var e in eList)
            {
                if (e.ExpressionValue is bool boolValue)
                {
                    var folded = boolValue ? SqlBoolTrue : SqlBoolFalse;
                    if (type == "$and" && folded == SqlBoolFalse)
                    {
                        predicateSql = SqlBoolFalse;
                        return true;
                    }
                    if (type == "$or" && folded == SqlBoolTrue)
                    {
                        predicateSql = SqlBoolTrue;
                        return true;
                    }
                    if ((type == "$and" && folded == SqlBoolTrue) || (type == "$or" && folded == SqlBoolFalse))
                        continue;
                }

                if (!TryBuildDirectValueExistsPredicate(e, out var part))
                    return false;
                parts.Add(part);
            }

            if (parts.Count == 0)
            {
                predicateSql = type == "$and" ? SqlBoolTrue : SqlBoolFalse;
                return true;
            }

            predicateSql = parts.Count == 1
                ? parts[0]
                : "(" + string.Join(type == "$and" ? " AND " : " OR ", parts) + ")";
            return true;
        }

        return false;
    }

    private static bool TryBuildDirectValueExistsPredicate(LogicalExpression le, out string predicateSql)
    {
        predicateSql = "";

        if (le.ExpressionValue is List<LogicalExpression> eList)
        {
            if (le.ExpressionType is "$and" or "$or")
                return TryBuildDirectValueExistsPredicate(eList, le.ExpressionType, out predicateSql);

            if (le.ExpressionType is "$eq" or "$ne" or "$gt" or "$ge" or "$lt" or "$le" or "$starts-with" or "$ends-with" or "$contains")
            {
                if (eList.Count < 2)
                    return false;

                var leftNode = eList[0];
                var rightNode = eList[1];
                if (leftNode.ExpressionType != "$field" || leftNode.ExpressionValue is not string rawField || rawField != "$sme#value")
                    return false;

                var smeValue = rightNode.ExpressionType switch
                {
                    "$strVal" => "svalue",
                    "$numVal" => "mvalue",
                    "$dateTimeVal" => "dtvalue",
                    "$hexVal" => "mvalue",
                    _ => ""
                };
                var leftSql = smeValue switch
                {
                    "svalue" => "v.\"SValue\"",
                    "mvalue" => "v.\"NValue\"",
                    "dtvalue" => "v.\"DTValue\"",
                    _ => "$SKIP"
                };
                if (leftSql == "$SKIP")
                    return false;

                var rightSql = BuildScopeLiteralSql(rightNode, smeValue);
                if (rightSql == "$SKIP" || rightSql == null)
                    return false;

                var combined = CombineComparisonSql(leftSql, le.ExpressionType, rightSql);
                if (combined == null)
                    return false;

                predicateSql = combined;
                return true;
            }
        }

        return false;
    }

    private static string? BuildOverallComparisonSql(List<LogicalExpression> eList, string type, SqlBuildContext ctx)
    {
        if (eList.Count < 2) return "$SKIP";
        var leftNode  = eList[0];
        var rightNode = eList[1];

        var smeValue = leftNode.ExpressionType == "$strVal" || rightNode.ExpressionType == "$strVal" ? "svalue"
                     : leftNode.ExpressionType == "$numVal" || rightNode.ExpressionType == "$numVal" ? "mvalue"
                     : leftNode.ExpressionType == "$dateTimeVal" || rightNode.ExpressionType == "$dateTimeVal" ? "dtvalue"
                     : leftNode.ExpressionType == "$hexVal" || rightNode.ExpressionType == "$hexVal" ? "mvalue"
                     : "";

        // $attribute(CLAIM(...)) operands resolve to a token-claim value — not a column. Emit a
        // sentinel SQL string literal so the comparison is well-formed; per-request substitution
        // in SqlConditions.SubstituteTokenClaims replaces the sentinel with the real claim value
        // before SQL execution. Skipping the branch (as the previous code did) silently dropped
        // the CLAIM check and made CLAIM-gated rules permissive — see SecurityService refactor 4bef43ac.
        if (leftNode.ExpressionType == "$attribute" || rightNode.ExpressionType == "$attribute")
        {
            var leftSqlClaim  = BuildClaimOrLiteralSql(leftNode, smeValue);
            if (leftSqlClaim == "$SKIP" || leftSqlClaim == null) return "$SKIP";
            var rightSqlClaim = BuildClaimOrLiteralSql(rightNode, smeValue);
            if (rightSqlClaim == "$SKIP" || rightSqlClaim == null) return "$SKIP";
            return CombineComparisonSql(leftSqlClaim, type, rightSqlClaim);
        }

        // Check for path condition ($sme.idShortPath)
        if (leftNode.ExpressionType == "$field" && leftNode.ExpressionValue is string rawField
            && rawField.Contains("$sme."))
        {
            var pathSql = TryBuildStandalonePathSql(leftNode, rightNode, type, smeValue, ctx);
            if (pathSql != null) return pathSql;
            return "$SKIP";
        }

        var leftSql  = BuildOverallFieldSql(leftNode, smeValue);
        if (leftSql == "$SKIP" || leftSql == null) return "$SKIP";

        var rightSql = BuildScopeLiteralSql(rightNode, smeValue);
        if (rightSql == "$SKIP" || rightSql == null) return "$SKIP";

        return CombineComparisonSql(leftSql, type, rightSql);
    }

    /// <summary>
    /// Maps a comparison operand to overall-SQL: a literal for <c>$strVal</c>/<c>$numVal</c>/...,
    /// or a deferred claim sentinel literal for <c>$attribute(CLAIM(&lt;type&gt;))</c>.
    /// </summary>
    private static string? BuildClaimOrLiteralSql(LogicalExpression node, string smeValue)
    {
        if (node.ExpressionType == "$attribute")
        {
            if (node.ExpressionValue is LogicalExpression inner
                && inner.ExpressionType == "CLAIM"
                && inner.ExpressionValue is string claimType
                && !string.IsNullOrEmpty(claimType))
            {
                return SqlConditions.BuildClaimSentinelSqlLiteral(claimType);
            }
            return "$SKIP";
        }
        return BuildScopeLiteralSql(node, smeValue);
    }

    /// <summary>Builds a standalone path placeholder in FormulaConditions["all"], adds PathJoin to Sc.Paths.</summary>
    private static string? TryBuildStandalonePathSql(
        LogicalExpression fieldNode, LogicalExpression valueNode, string type, string smeValue, SqlBuildContext ctx)
    {
        if (!TryBuildPathSubquerySql(fieldNode, valueNode, type, smeValue, smeAlias: "sme",
                innerJoin: false, out var pathSql, out var idShortPath))
            return null;

        var idx = ctx.PathIndex++;
        var pj = new PathJoin { Placeholder = $"path{idx}", SubquerySql = pathSql, IdShortPath = idShortPath };
        ctx.Sc.Paths.Add(pj);
        return $"$${pj.Placeholder}$$";
    }

    /// <summary>For a $match child: builds PathJoin with body SQL using "sme" as alias (replaced later).</summary>
    private static PathJoin? TryBuildMatchPathJoin(LogicalExpression le)
    {
        if (le.ExpressionValue is not List<LogicalExpression> eList || eList.Count < 2)
            return null;
        var type      = le.ExpressionType;
        var leftNode  = eList[0];
        var rightNode = eList[1];

        if (leftNode.ExpressionType != "$field" || leftNode.ExpressionValue is not string raw
            || !raw.Contains("$sme."))
            return null;

        var smeValue = leftNode.ExpressionType == "$strVal" || rightNode.ExpressionType == "$strVal" ? "svalue"
                     : leftNode.ExpressionType == "$numVal" || rightNode.ExpressionType == "$numVal" ? "mvalue"
                     : leftNode.ExpressionType == "$dateTimeVal" || rightNode.ExpressionType == "$dateTimeVal" ? "dtvalue"
                     : leftNode.ExpressionType == "$hexVal"  || rightNode.ExpressionType == "$hexVal" ? "mvalue"
                     : "";

        if (!TryBuildPathSubquerySql(leftNode, rightNode, type, smeValue, smeAlias: "sme",
                innerJoin: true, out var sql, out var idShortPath))
            return null;

        return new PathJoin { SubquerySql = sql, IdShortPath = idShortPath };
    }

    /// <summary>
    /// Builds the body (FROM … WHERE …) of a LEFT-JOIN subquery for one $sme.idShortPath#field comparison.
    /// <paramref name="smeAlias"/> is the alias used for SMESets inside the subquery (no SELECT header — callers add it).
    /// </summary>
    private static bool TryBuildPathSubquerySql(
        LogicalExpression fieldNode, LogicalExpression valueNode,
        string type, string smeValue, string smeAlias,
        bool innerJoin,
        out string sql, out string idShortPath)
    {
        sql = "";
        idShortPath = "";

        if (fieldNode.ExpressionValue is not string raw || !raw.Contains("$sme."))
            return false;

        // raw format: "$sme.{idShortPath}#{fieldName}"
        var withoutPrefix = raw.Replace("$sme.", "");
        var parts = withoutPrefix.Split('#');
        if (parts.Length != 2) return false;
        idShortPath  = parts[0];
        var fieldName = parts[1];   // e.g. "value", "idShort", "valueType", "language"

        var idShort = idShortPath.Contains('.') ? idShortPath.Split('.').Last() : idShortPath;

        // Value SQL for the JOIN condition
        var valueSql = BuildPathValueSql(fieldName, valueNode, type, smeValue, smeAlias);
        if (valueSql == null) return false;

        // IdShort and IdShortPath SQL
        var idShortSql     = $"\"{smeAlias}\".\"IdShort\" = '{EscSql(idShort)}'";
        var idShortPathSql = BuildIdShortPathSql(smeAlias, idShortPath);

        // Build body only (no SELECT header)
        var joinKeyword = innerJoin ? "JOIN" : "LEFT JOIN";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"FROM SMESets {smeAlias}");
        sb.AppendLine($"{joinKeyword} ValueSets v ON v.SMEId = {smeAlias}.Id AND {valueSql}");
        sb.AppendLine($"WHERE {valueSql}");
        // Skip IdShort condition for wildcard paths (%, []) — mirrors old behaviour
        var hasWildcard = idShort.Contains('%') || idShort.Contains('[');
        if (!hasWildcard)
            sb.AppendLine($"AND {idShortSql}");
        sb.Append($"AND {idShortPathSql}");

        sql = sb.ToString();
        return true;
    }

    private static string BuildIdShortPathSql(string smeAlias, string idShortPath)
    {
        if (idShortPath.Contains("[]"))
        {
            var pattern = idShortPath.Replace("[]", "[[]*[]]");
            return $"\"{smeAlias}\".\"IdShortPath\" GLOB '{EscSql(pattern)}'";
        }
        if (idShortPath.Contains('%'))
        {
            var pattern = idShortPath.Replace('%', '*');
            return $"\"{smeAlias}\".\"IdShortPath\" GLOB '{EscSql(pattern)}'";
        }
        return $"\"{smeAlias}\".\"IdShortPath\" = '{EscSql(idShortPath)}'";
    }

    /// <summary>Builds the SQL condition for the value column(s) of a path join.</summary>
    private static string? BuildPathValueSql(
        string fieldName, LogicalExpression valueNode, string type, string smeValue, string smeAlias)
    {
        // vtvalue / langvalue: two-column predicates (TValue + Annotation)
        if (fieldName == "valueType")
        {
            var litSql = BuildScopeLiteralSql(valueNode, smeValue);
            if (litSql == null || litSql == "$SKIP") return null;
            var literal = UnquoteSqlLiteral(litSql);
            if (!SmeQueryPrefilter.TryGetTValueForValueTypeLiteral(literal, out var disc)) return null;
            if (!SmeQueryPrefilter.TrySerializeDataTypeAnnotation(literal, out var ann)) return null;
            var discEsc = EscSql(disc);
            var annEsc  = EscSql(ann);
            return type switch
            {
                "$eq" => $"(\"{smeAlias}\".\"TValue\" = '{discEsc}' AND v.\"Annotation\" = '{annEsc}')",
                "$ne" => $"(\"{smeAlias}\".\"TValue\" <> '{discEsc}' OR v.\"Annotation\" <> '{annEsc}')",
                _ => null
            };
        }
        if (fieldName == "language")
        {
            var litSql = BuildScopeLiteralSql(valueNode, smeValue);
            if (litSql == null || litSql == "$SKIP") return null;
            var literal = UnquoteSqlLiteral(litSql);
            if (!LanguageQueryPrefilter.TryValidateLanguageLiteral(literal, out var lang)) return null;
            var langEsc = EscSql(lang);
            return type switch
            {
                "$eq" => $"(\"{smeAlias}\".\"TValue\" = 'S' AND v.\"Annotation\" = '{langEsc}')",
                "$ne" => $"(\"{smeAlias}\".\"TValue\" <> 'S' OR v.\"Annotation\" <> '{langEsc}')",
                "$contains" => $"(\"{smeAlias}\".\"TValue\" = 'S' AND v.\"Annotation\" LIKE '%{EscSql(lang)}%')",
                "$starts-with" => $"(\"{smeAlias}\".\"TValue\" = 'S' AND v.\"Annotation\" LIKE '{EscSql(lang)}%')",
                "$ends-with" => $"(\"{smeAlias}\".\"TValue\" = 'S' AND v.\"Annotation\" LIKE '%{EscSql(lang)}')",
                _ => null
            };
        }

        // Standard value column
        var sqlCol = fieldName switch
        {
            "value"  => smeValue switch
            {
                "svalue"  => "v.\"SValue\"",
                "mvalue"  => "v.\"NValue\"",
                "dtvalue" => "v.\"DTValue\"",
                _ => "v.\"SValue\""   // default
            },
            "idShort"     => $"\"{smeAlias}\".\"IdShort\"",
            "idShortPath" => $"\"{smeAlias}\".\"IdShortPath\"",
            _ => null
        };
        if (sqlCol == null) return null;

        var rhs = BuildScopeLiteralSql(valueNode, smeValue);
        if (rhs == "$SKIP" || rhs == null) return null;

        return CombineComparisonSql(sqlCol, type, rhs);
    }

    // ------------------------------------------------------------------
    // Overall condition: field→SQL mapping (with table prefix a. / t.)
    // ------------------------------------------------------------------
    private static string? BuildOverallFieldSql(LogicalExpression node, string smeValue)
    {
        if (node.ExpressionType != "$field" || node.ExpressionValue is not string raw)
            return "$SKIP";

        if (raw.Contains("$sme.")) return "$SKIP"; // handled as path

        var vNorm = raw.StartsWith("$sme#") ? raw.Replace("$sme#", "sme.") : raw;
        if (vNorm == "sme.valueType") return "vtvalue"; // handled by TryResolveSmeValueTypeLanguage
        if (vNorm == "sme.language") return "langvalue";

        var field = raw == "$aas#id" ? "$aas#identifier"
                  : raw == "$sm#id"  ? "$sm#identifier"
                  : raw;

        if (field.StartsWith("$aas#"))
        {
            var col = AasColumnSql(field["$aas#".Length..]);
            return col == null ? "$SKIP" : $"\"a\".{col}";
        }
        if (field.StartsWith("$sm#"))
        {
            var col = SmColumnSql(field["$sm#".Length..]);
            return col == null ? "$SKIP" : $"\"t\".{col}";
        }
        if (field.StartsWith("$sme#"))
        {
            var prop = field["$sme#".Length..];
            if (prop == "value")
            {
                return smeValue switch
                {
                    "svalue"  => "\"v\".\"SValue\"",
                    "mvalue"  => "\"v\".\"NValue\"",
                    "dtvalue" => "\"v\".\"DTValue\"",
                    _ => "$SKIP"
                };
            }
            var col = SmeColumnSql(prop);
            return col == null ? "$SKIP" : $"\"s1\".{col}";
        }
        return "$SKIP";
    }

    // ------------------------------------------------------------------
    // Literal → SQL RHS
    // ------------------------------------------------------------------
    private static string? BuildScopeLiteralSql(LogicalExpression node, string smeValue)
    {
        switch (node.ExpressionType)
        {
            case "$strVal":
                if (node.ExpressionValue is string sv)
                {
                    if (sv == "$null") return "NULL";
                    return $"'{EscSql(sv)}'";
                }
                return "$SKIP";
            case "$numVal":
                if (node.ExpressionValue is int or long or double)
                    return System.Convert.ToString(node.ExpressionValue, System.Globalization.CultureInfo.InvariantCulture);
                return "$SKIP";
            case "$dateTimeVal":
                if (node.ExpressionValue is string dv)
                    return $"'{EscSql(dv)}'";
                return "$SKIP";
            case "$hexVal":
                if (node.ExpressionValue is string hv)
                {
                    var hex = hv.Replace("16#", "");
                    var num = System.Convert.ToInt64(hex, 16);
                    var d = (double)num;
                    var s = d.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
                    if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e')) s += ".0";
                    return s;
                }
                return "$SKIP";
        }
        return "$SKIP";
    }

    // ------------------------------------------------------------------
    // Comparison combiner
    // ------------------------------------------------------------------
    private static string? CombineComparisonSql(string leftSql, string type, string rightSql)
    {
        if (rightSql == "NULL")
        {
            return type switch
            {
                "$eq" => $"{leftSql} IS NULL",
                "$ne" => $"{leftSql} IS NOT NULL",
                _ => null
            };
        }
        var sqlOp = type switch
        {
            "$eq" => "=", "$ne" => "<>",
            "$gt" => ">", "$ge" => ">=", "$lt" => "<", "$le" => "<=",
            "$starts-with" => null, "$ends-with" => null, "$contains" => null,
            _ => null
        };
        if (sqlOp != null)
            return $"({leftSql} {sqlOp} {rightSql})";

        // String methods — rightSql is already 'literal' with quotes
        var inner = EscSql(UnquoteSqlLiteral(rightSql));
        return type switch
        {
            "$starts-with" => $"({leftSql} GLOB '{inner}*')",
            "$ends-with"   => $"({leftSql} GLOB '*{inner}')",
            "$contains"    => $"({leftSql} GLOB '*{inner}*')",
            _ => null
        };
    }

    // ------------------------------------------------------------------
    // Column name mappings
    // ------------------------------------------------------------------
    private static string? AasColumnSql(string prop) => prop switch
    {
        "identifier" => "\"Identifier\"",
        "idShort"    => "\"IdShort\"",
        "category"   => "\"Category\"",
        _ => null
    };

    private static string? SmColumnSql(string prop) => prop switch
    {
        "identifier" => "\"Identifier\"",
        "idShort"    => "\"IdShort\"",
        "category"   => "\"Category\"",
        "semanticId" => "\"SemanticId\"",
        _ => null
    };

    private static string? SmeColumnSql(string prop) => prop switch
    {
        "idShort"     => "\"IdShort\"",
        "idShortPath" => "\"IdShortPath\"",
        "category"    => "\"Category\"",
        "semanticId"  => "\"SemanticId\"",
        _ => null
    };

    // ------------------------------------------------------------------
    // SQL string helpers
    // ------------------------------------------------------------------
    private static string EscSql(string s) => s.Replace("'", "''");

    private static string UnquoteSqlLiteral(string sql)
    {
        if (sql.Length >= 2 && sql.StartsWith('\'') && sql.EndsWith('\''))
            return sql[1..^1].Replace("''", "'");
        return sql;
    }
}
