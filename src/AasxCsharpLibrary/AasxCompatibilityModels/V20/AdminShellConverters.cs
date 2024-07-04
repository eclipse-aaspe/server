/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using AasxCompatibilityModels;

namespace AdminShell_V20;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class AdminShellConverters
{
    /// <summary>
    /// This converter is used for reading JSON files; it claims to be responsible for
    /// "Referable" (the base class)
    /// and decides, which subclass of the base class shall be populated.
    /// If the object is SubmodelElement, the decision, which special subclass to create is done in a factory
    /// AdminShell.SubmodelElementWrapper.CreateAdequateType(),
    /// in order to have all subclass specific decisions in one place (SubmodelElementWrapper)
    /// Remark: There is a NuGet package JsonSubTypes, which could have done the job, except the fact of having
    /// "modelType" being a class property with a contained property "name".
    /// </summary>
    public class JsonAasxConverter : JsonConverter<AdminShell.Referable>
    {
        public override AdminShellV20.Referable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc  = JsonDocument.ParseValue(ref reader);
            var       root = doc.RootElement;

            var target = new AdminShellV20.Referable();

            // Check if the root element has the necessary properties
            if (root.TryGetProperty("idShort", out JsonElement idShortElement))
            {
                target.idShort = idShortElement.GetString();
            }

            if (root.TryGetProperty("category", out JsonElement categoryElement))
            {
                target.category = categoryElement.GetString();
            }

            if (!root.TryGetProperty("description", out JsonElement descriptionElement))
            {
                return target;
            }

            var langStringArray = descriptionElement.GetProperty("langString").EnumerateArray();

            foreach (var langString in langStringArray)
            {
                var lang = langString.GetProperty("lang").GetString();
                var str  = langString.GetProperty("str").GetString();
                target.AddDescription(lang, str);
            }

            return target;
        }
        
        public override void Write(Utf8JsonWriter writer, AdminShellV20.Referable value, JsonSerializerOptions options) => throw new NotImplementedException();

        public override bool CanConvert(Type typeToConvert) => typeof(AdminShellV20.Referable).IsAssignableFrom(typeToConvert);
    }

    public class AdaptiveFilterContractResolver : JsonConverter<AdminShell.AdministrationShell>
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

        public override AdminShellV20.AdministrationShell Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, AdminShellV20.AdministrationShell value, JsonSerializerOptions options) => throw new NotImplementedException();

        public override bool CanConvert(Type typeToConvert) => typeof(AdminShellV20.AdministrationShell).IsAssignableFrom(typeToConvert);
    }
}