using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

public class Value
{
    [JsonProperty("$field")]
    public string Field { get; set; }

    [JsonProperty("$strVal")]
    public string StrVal { get; set; }

    [JsonProperty("$attribute")]
    public AttributeItem Attribute { get; set; }

    [JsonProperty("$numVal")]
    public double? NumVal { get; set; }

    [JsonProperty("$hexVal")]
    public string HexVal { get; set; }

    [JsonProperty("$dateTimeVal")]
    public DateTime? DateTimeVal { get; set; }

    [JsonProperty("$timeVal")]
    public string TimeVal { get; set; }

    [JsonProperty("$boolean")]
    public bool? Boolean { get; set; }

    [JsonProperty("$strCast")]
    public Value StrCast { get; set; }

    [JsonProperty("$numCast")]
    public Value NumCast { get; set; }

    [JsonProperty("$hexCast")]
    public Value HexCast { get; set; }

    [JsonProperty("$boolCast")]
    public Value BoolCast { get; set; }

    [JsonProperty("$dateTimeCast")]
    public Value DateTimeCast { get; set; }

    [JsonProperty("$timeCast")]
    public Value TimeCast { get; set; }

    [JsonProperty("$dayOfWeek")]
    public DateTime? DayOfWeek { get; set; }

    [JsonProperty("$dayOfMonth")]
    public DateTime? DayOfMonth { get; set; }

    [JsonProperty("$month")]
    public DateTime? Month { get; set; }

    [JsonProperty("$year")]
    public DateTime? Year { get; set; }
}

public class AttributeItem
{
    [JsonIgnore] public string ItemType { get; set; }
    [JsonIgnore] public string Value { get; set; }

    [JsonExtensionData] private IDictionary<string, JToken> _data;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (_data != null && _data.Count == 1)
        {
            var kv = _data.First();
            ItemType = kv.Key;
            Value = kv.Value.ToObject<string>();

            _data = null;
        }
    }
}

public class Query
{
    [JsonProperty("$select")]
    public string Select { get; set; }

    [JsonProperty("$condition")]
    public LogicalExpression Condition { get; set; }

    public Dictionary<string, string> _query_conditions = [];
}

public class LogicalExpression
{
    [JsonIgnore] public string ExpressionType { get; set; }
    [JsonIgnore] public object ExpressionValue { get; set; }

    [JsonExtensionData] private IDictionary<string, JToken> _data;

    public string _expression = "";

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (_data != null && _data.Count == 1)
        {
            var kv = _data.First();
            ExpressionType = kv.Key;
            ExpressionValue = kv.Value.Type switch
            {
                JTokenType.Array => kv.Value.ToObject<List<LogicalExpression>>() ?? (object)kv.Value.ToObject<List<Value>>() ?? kv.Value.ToObject<List<StringValue>>(),
                JTokenType.Object => kv.Value.ToObject<LogicalExpression>(),
                JTokenType.Boolean => kv.Value.ToObject<bool>(),
                _ => kv.Value.ToObject<object>()
            };

            _data = null;
        }
    }
}

public class StringValue
{
    [JsonProperty("$field")]
    public string Field { get; set; }

    [JsonProperty("$strVal")]
    public string StrVal { get; set; }

    [JsonProperty("$strCast")]
    public Value StrCast { get; set; }

    [JsonProperty("$attribute")]
    public AttributeItem Attribute { get; set; }
}

public class Root
{
    [JsonProperty("Query")]
    public Query Query { get; set; }

    [JsonProperty("AllAccessPermissionRules")]
    public AllAccessPermissionRules AllAccessPermissionRules { get; set; }
}

public class AllAccessPermissionRules
{
    [JsonProperty("DEFATTRIBUTES")]
    public List<DefAttribute> DefAttributes { get; set; }

    [JsonProperty("DEFACLS")]
    public List<DefAcl> DefAcls { get; set; }

    [JsonProperty("DEFOBJECTS")]
    public List<DefObject> DefObjects { get; set; }

    [JsonProperty("DEFFORMULAS")]
    public List<DefFormula> DefFormulas { get; set; }

    [JsonProperty("rules")]
    public List<AccessPermissionRule> Rules { get; set; }
}

public class DefAttribute
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("attributes")]
    public List<AttributeItem> Attributes { get; set; }
}

public class DefAcl
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("acl")]
    public ACL Acl { get; set; }
}

public class DefObject
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("objects")]
    public List<ObjectItem> Objects { get; set; }

    [JsonProperty("USEOBJECTS")]
    public List<string> UseObjects { get; set; }
}

public class DefFormula
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("formula")]
    public LogicalExpression Formula { get; set; }
}

public class AccessPermissionRule
{
    [JsonProperty("ACL")]
    public ACL? Acl { get; set; }

    [JsonProperty("USEACL")]
    public string? UseAcl { get; set; }

    [JsonProperty("OBJECTS")]
    public List<ObjectItem> Objects { get; set; }

    [JsonProperty("USEOBJECTS")]
    public List<string>? UseObjects { get; set; }

    [JsonProperty("FORMULA")]
    public LogicalExpression Formula { get; set; }

    [JsonProperty("USEFORMULA")]
    public string? UseFormula { get; set; }

    [JsonProperty("FILTER")]
    public Filter? Filter { get; set; }

    [JsonProperty("USEFILTER")]
    public string? UseFilter { get; set; }

    public Dictionary<string, string> _formula_conditions = [];
    public Dictionary<string, string> _filter_conditions = [];
}
public class Filter
{
    [JsonProperty("FRAGMENT")]
    public string Fragment { get; set; }

    [JsonProperty("CONDITION")]
    public LogicalExpression Condition { get; set; }
}
public class ACL
{
    [JsonProperty("ATTRIBUTES")]
    public List<AttributeItem> Attributes { get; set; }

    [JsonProperty("USEATTRIBUTES")]
    public string UseAttributes { get; set; }

    [JsonProperty("RIGHTS")]
    public List<String> Rights { get; set; }

    [JsonProperty("ACCESS")]
    public string Access { get; set; }
}

public class ObjectItem
{
    [JsonIgnore] public string ItemType { get; set; }
    [JsonIgnore] public string Value { get; set; }

    [JsonExtensionData] private IDictionary<string, JToken> _data;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (_data != null && _data.Count == 1)
        {
            var kv = _data.First();
            ItemType = kv.Key;
            Value = kv.Value.ToObject<string>();

            _data = null;
        }
    }
}
