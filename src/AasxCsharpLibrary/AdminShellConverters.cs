/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace AdminShellNS;

public static class AdminShellConverters
{
    /// <summary>
    /// This converter is used for reading JSON files; it claims to be responsible for
    /// "Referable" (the base class)
    /// and decides, which subclass of the base class shall be populated.
    /// If the object is SubmodelElement, the decision, which special subclass to create is done in a factory
    /// SubmodelElementWrapper.CreateAdequateType(),
    /// in order to have all subclass specific decisions in one place (SubmodelElementWrapper)
    /// Remark: There is a NuGet package JsonSubTypes, which could have done the job, except the fact of having
    /// "modelType" being a class property with a contained property "name".
    /// </summary>
    public class JsonAasxConverter : JsonConverter
    {
        private string UpperClassProperty = "modelType";
        private string LowerClassProperty = "name";

        public JsonAasxConverter(string upperClassProperty, string lowerClassProperty)
        {
            UpperClassProperty = upperClassProperty;
            LowerClassProperty = lowerClassProperty;
        }

        public override bool CanConvert(Type objectType) => typeof(IReferable).IsAssignableFrom(objectType);

        public override bool CanWrite => false;

        public override object? ReadJson(JsonReader reader,
                                         Type objectType,
                                         object existingValue,
                                         JsonSerializer serializer)
        {
            // Load JObject from stream
            var jObject = JObject.Load(reader);

            // Create target object based on JObject
            IReferable? target = null;

            if (jObject.ContainsKey(UpperClassProperty))
            {
                var j2 = jObject[UpperClassProperty];
                if (j2 != null)
                    foreach (var c in j2.Children())
                    {
                        if (c is not JProperty cprop)
                        {
                            continue;
                        }

                        if (cprop.Name != LowerClassProperty || cprop.Value.Type.ToString() != "String")
                        {
                            continue;
                        }

                        var cpval = cprop.Value.ToObject<string>();
                        if (cpval == null)
                        {
                            continue;
                        }

                        // Info MIHO 21 APR 2020: use Referable.CreateAdequateType instead of SMW...
                        var o = CreateAdequateType(cpval);
                        if (o != null)
                        {
                            target = o;
                        }
                    }
            }

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }

        public static IReferable? CreateAdequateType(string elementName)
        {
            if (elementName == KeyTypes.AssetAdministrationShell.ToString())
            {
                return new AssetAdministrationShell("", null);
            }

            if (elementName == KeyTypes.ConceptDescription.ToString())
            {
                return new ConceptDescription("");
            }

            if (elementName == KeyTypes.Submodel.ToString())
            {
                return new Submodel("");
            }

            return CreateSubmodelElementInstance(elementName);
        }

        private static ISubmodelElement? CreateSubmodelElementInstance(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null || !type.IsSubclassOf(typeof(ISubmodelElement)))
            {
                return null;
            }

            var sme = Activator.CreateInstance(type) as ISubmodelElement;
            return sme;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }

    /// <summary>
    /// This converter / contract resolver for Json.NET adaptively filters different levels of depth
    /// of nested AASX structures.
    /// </summary>
    public class AdaptiveFilterContractResolver : DefaultContractResolver
    {
        public bool AasHasViews = true;
        public bool BlobHasValue = true;
        public bool SubmodelHasElements = true;
        public bool SmcHasValue = true;
        public bool OpHasVariables = true;

        public AdaptiveFilterContractResolver() { }

        public AdaptiveFilterContractResolver(bool deep = true, bool complete = true)
        {
            if (!deep)
            {
                SubmodelHasElements = false;
                SmcHasValue         = false;
                OpHasVariables      = false;
            }

            if (complete)
            {
                return;
            }

            AasHasViews  = false;
            BlobHasValue = false;

        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (!BlobHasValue && property.DeclaringType == typeof(Blob) &&
                property.PropertyName == "value")
            {
                property.ShouldSerialize = _ => { return false; };
            }

            if (!SubmodelHasElements && property.DeclaringType == typeof(Submodel) &&
                property.PropertyName == "submodelElements")
            {
                property.ShouldSerialize = _ => { return false; };
            }

            if (!SmcHasValue && property.DeclaringType == typeof(SubmodelElementCollection) &&
                property.PropertyName == "value")
            {
                property.ShouldSerialize = _ => { return false; };
            }

            if (!OpHasVariables && property.DeclaringType == typeof(Operation) &&
                (property.PropertyName == "in" || property.PropertyName == "out"))
            {
                property.ShouldSerialize = _ => { return false; };
            }

            if (!AasHasViews && property.DeclaringType == typeof(AssetAdministrationShell) &&
                property.PropertyName == "views")
            {
                property.ShouldSerialize = _ => { return false; };
            }

            return property;
        }
    }

}