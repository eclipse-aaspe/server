using Irony.Parsing;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;
using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class JsonParser
{
    public void PrintParseTree(string jsonText)
    {
        var sw = new StringWriter();
        PrintParseTree(jsonText, null, 0, sw);
    }
    public void PrintParseTree(string jsonText, JsonNode node, int indent, StringWriter sw)
    {
        string errorText = "";

        // Read schema.json
        string schemaText = File.ReadAllText("schema.json");

        var task = Task.Run(async () =>
        {
            var schema = await JsonSchema.FromJsonAsync(schemaText);

            // Validate JSON against schema
            try
            {
                var errors = schema.Validate(jsonText);
                if (errors.Count == 0)
                {
                    Console.WriteLine("JSON is valid.");
                    var json = JsonNode.Parse(jsonText);
                    IterateJson(json);
                }
                else
                {
                    Console.WriteLine("JSON is invalid. Errors:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ToString());
                        errorText += error.ToString() + "\n";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON is invalid. Errors:");
                Console.WriteLine(ex.Message);
                errorText += ex.Message + "\n";
            }
        }
        );
        task.Wait();

        sw.WriteLine(errorText);
    }

    static void IterateJson(JsonNode node, string indent = "")
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                Console.WriteLine($"{indent}{property.Key}:");
                IterateJson(property.Value, indent + "  ");
            }
        }
        else if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                // Console.WriteLine($"{indent}[{i}]:");
                IterateJson(array[i], indent + "  ");
            }
        }
        else
        {
            Console.WriteLine($"{indent}{node} : {node.GetValueKind()}");
        }
    }

    public string ParseTreeToExpression(string jsonText, string typePrefix, ref int upperCountTypePrefix)
    {
        upperCountTypePrefix = 0;
        string errorText = "";

        // Read schema.json
        string schemaText = File.ReadAllText("schema.json");

        var task = Task.Run(async () =>
        {
            var schema = await JsonSchema.FromJsonAsync(schemaText);

            // Validate JSON against schema
            try
            {
                var errors = schema.Validate(jsonText);
                if (errors.Count != 0)
                {
                    Console.WriteLine("JSON is invalid. Errors:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ToString());
                        errorText += error.ToString() + "\n";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON is invalid. Errors:");
                Console.WriteLine(ex.Message);
                errorText += ex.Message + "\n";
            }
        }
        );
        task.Wait();

        if (errorText == "")
        {
            if (typePrefix == "")
            {
                Console.WriteLine("JSON is valid.");
            }
            var json = JsonNode.Parse(jsonText);

            string argumentType = "";
            return ParseTreeToExpression(json, typePrefix, ref upperCountTypePrefix, ref argumentType);
            // IterateJson(json);
        }

        throw new Exception(errorText);

        return "";
    }

    public string ParseTreeToExpression(JsonNode node, string typePrefix, ref int upperCountTypePrefix, ref string argumentType, string parentName = "")
    {
        if (node == null)
            return "";

        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                // Console.WriteLine($"{indent}{property.Key}:");
                // IterateJson(property.Value, indent + "  ");
                string arg1 = "";
                string arg2 = "";
                string op = "";
                bool strOp = false;
                bool numOp = false;
                int countTypePrefix1 = 0;
                int countTypePrefix2 = 0;
                int pattern = -1;
                string expression = "";

                switch (property.Key)
                {
                    case "queryParameter":
                        string dummyArgumentType = "";
                        expression = ParseTreeToExpression(property.Value, typePrefix, ref upperCountTypePrefix, ref dummyArgumentType);
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
                    case "$not":
                        op = "!";
                        pattern = 0;
                        break;
                    case "$or":
                        op = "||";
                        pattern = 1;
                        break;
                    case "$and":
                        op = "&&";
                        pattern = 1;
                        break;
                    case "$eq":
                        op = "==";
                        pattern = 2;
                        break;
                    case "$ne":
                        op = "!=";
                        pattern = 2;
                        break;
                    case "$gt":
                        op = ">";
                        pattern = 2;
                        break;
                    case "$ge":
                        op = ">=";
                        pattern = 2;
                        break;
                    case "$lt":
                        op = "<";
                        pattern = 2;
                        break;
                    case "$le":
                        op = "<=";
                        pattern = 2;
                        break;
                    case "$contains":
                        op = "Contains";
                        strOp = true;
                        pattern = 3;
                        break;
                    case "$starts-with":
                        op = "StartsWith";
                        strOp = true;
                        pattern = 3;
                        break;
                    case "ends-with":
                        op = "EndsWith";
                        strOp = true;
                        pattern = 3;
                        break;
                }

                if (strOp && typePrefix == "num()")
                {
                    return "$SKIP";
                }

                if (pattern == 0) // bool single
                {
                    string argumentTypeDummy = "";
                    arg1 = ParseTreeToExpression(property.Value.AsObject(), typePrefix, ref countTypePrefix1, ref argumentTypeDummy);
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
                }
                else if (pattern == 1) // bool multiple
                {
                    List<string> args = new List<string>();
                    List<int> count = new List<int>();
                    string argumentTypeDummy = "";
                    args.Add(ParseTreeToExpression(property.Value.AsArray()[0], typePrefix, ref countTypePrefix1, ref argumentTypeDummy));
                    count.Add(countTypePrefix1);
                    for (int i = 1; i < property.Value.AsArray().Count; i++)
                    {
                        countTypePrefix2 = 0;
                        args.Add(ParseTreeToExpression(property.Value.AsArray()[i], typePrefix, ref countTypePrefix2, ref argumentTypeDummy));
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
                        }
                    }

                    // and
                    if (property.Key == "$and")
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
                    if (property.Key == "$or")
                    {
                        if (trueCount != 0)
                        {
                            return "$TRUE";
                        }
                        if (falseCount == args.Count)
                        {
                            return "$FALSE";
                        }
                    }

                    expression = args[0];
                    for (int i = 1; i < args.Count; i++)
                    {
                        expression += op + args[i];
                    }
                    expression = "(" + expression + ")";
                    return expression;
                }
                else if (pattern == 2 || pattern == 3) // comparison or string
                {
                    string argumentType1 = "";
                    string argumentType2 = "";
                    arg1 = ParseTreeToExpression(property.Value.AsArray()[0], typePrefix, ref countTypePrefix1, ref argumentType1);
                    arg2 = ParseTreeToExpression(property.Value.AsArray()[1], typePrefix, ref countTypePrefix2, ref argumentType2);
                    if ((argumentType1 == "" || argumentType1 == typePrefix) && (argumentType2 == "" || argumentType2 == typePrefix))
                    {
                        upperCountTypePrefix += countTypePrefix1 + countTypePrefix2;
                    }
                    if (arg1 == "$SKIP" || arg2 == "$SKIP")
                    {
                        if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                        {
                            return "$TRUE";
                        }
                        return "$SKIP";
                    }
                    if (typePrefix == "num()" && (argumentType1 == "str()" || argumentType2 == "str()"))
                    {
                        /*
                        if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                        {
                            return "true";
                        }
                        */
                        return "$SKIP";
                    }
                    if (typePrefix == "str()" && (argumentType1 == "num()" || argumentType2 == "num()"))
                    {
                        /*
                        if (countTypePrefix1 != 0 || countTypePrefix2 != 0)
                        {
                            return "true";
                        }
                        */
                        return "$SKIP";
                    }
                    if (typePrefix == "" && (arg1 == "sme.value" || arg2 == "sme.value"))
                    {
                        string change = "";

                        if (argumentType1 == "num()" || argumentType2 == "num()")
                        {
                            change = "mvalue";
                        }
                        else
                        {
                            if (strOp || argumentType1 == "str()" || argumentType2 == "str()")
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

                    if (pattern == 2)
                    {
                        return "(" + arg1 + " " + op + " " +arg2 + ")";
                    }

                    if (pattern == 3)
                    {
                        return arg1 + "." + op + "(" + arg2 + ")";
                    }
                }
                return "$ERROR";
            }
        }
        else if (node is JsonArray array)
        {
            return "$ERROR";
            /*
            for (int i = 0; i < array.Count; i++)
            {
                // Console.WriteLine($"{indent}[{i}]:");
                // IterateJson(array[i], indent + "  ");
                ParseTreeToExpression(array[i], typePrefix, ref upperCountTypePrefix);
            }
            */
        }
        else
        {
            // Console.WriteLine($"{indent}{node} : {node.GetValueKind()}");
            var kind = node.GetValueKind().ToString();
            var value = node.ToString();
            if (kind == "String")
            {
                if (value.StartsWith("$"))
                {
                    // Identifier
                    value = value.Substring(1);

                    if (value == "sm.id") // patch grammar vs database
                    {
                        value = "sm.identifier";
                    }

                    // complete expression for joined tables
                    if (typePrefix == "" && value == "sme.value")
                    {
                        switch (parentName)
                        {
                            case "string":
                                return "svalue";
                            case "numerical":
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
                }
                // String Constant
                argumentType = "str()";
                return "\"" + value + "\"";
            }
            else if (kind == "Number")
            {
                // Number Constant
                argumentType = "num()";
                return value;
            }
            else if (kind.ToLower() == "true" || kind.ToLower() == "false")
            {
                return value;
            }
            return "$ERROR";
        }

        return "$ERROR";
    }
}
