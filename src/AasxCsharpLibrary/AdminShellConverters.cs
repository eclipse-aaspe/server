/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdminShellNS;

using System.Linq;

public static class AdminShellConverters
{
    public class JsonAasxConverter : JsonConverter<IReferable>
    {
        private string UpperClassProperty { get; }
        private string LowerClassProperty { get; }

        public JsonAasxConverter(string upperClassProperty, string lowerClassProperty)
        {
            UpperClassProperty = upperClassProperty ?? throw new ArgumentNullException(nameof(upperClassProperty));
            LowerClassProperty = lowerClassProperty ?? throw new ArgumentNullException(nameof(lowerClassProperty));
        }

        public override bool CanConvert(Type typeToConvert) => typeof(IReferable).IsAssignableFrom(typeToConvert);

        public override IReferable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc  = JsonDocument.ParseValue(ref reader);
            var       root = doc.RootElement;

            IReferable? target = null;

            if (root.TryGetProperty(UpperClassProperty, out var upperClassProperty))
            {
                target = CreateReferableInstance(upperClassProperty);
            }

            if (target != null)
            {
                DeserializeReferableProperties(root, target);
            }

            return target;
        }

        private IReferable? CreateReferableInstance(JsonElement upperClassProperty) => upperClassProperty.EnumerateObject()
                                                                                                         .Where(lowerClassPropertyValue =>
                                                                                                                    lowerClassPropertyValue.Name == LowerClassProperty &&
                                                                                                                    lowerClassPropertyValue.Value.ValueKind == JsonValueKind.String)
                                                                                                         .Select(lowerClassPropertyValue =>
                                                                                                                     CreateAdequateType(lowerClassPropertyValue.Value.GetString()))
                                                                                                         .FirstOrDefault();

        private void DeserializeReferableProperties(JsonElement root, IReferable target)
        {
            DeserializeProperty(root, "category", JsonValueKind.String, value => target.Category    = value.GetString());
            DeserializeArrayProperty(root, "idShort", JsonValueKind.String, value => target.IdShort = value.GetString());
            DeserializeLangStringArrayProperty(root, "displayName", value => target.DisplayName     = value);
            DeserializeLangStringArrayProperty(root, "description", value => target.Description     = value);
        }

        private static void DeserializeProperty(JsonElement root, string propertyName, JsonValueKind expectedKind, Action<JsonElement> assignAction)
        {
            if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == expectedKind)
            {
                assignAction(property);
            }
        }

        public static void DeserializeArrayProperty(JsonElement root, string propertyName, JsonValueKind expectedKind, Action<JsonElement> assignAction)
        {
            if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == expectedKind)
            {
                assignAction(property);
            }
        }

        private static void DeserializeLangStringArrayProperty(JsonElement root, string propertyName, Action<List<ILangStringNameType>> assignAction)
        {
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var list = new List<ILangStringNameType>();
            foreach (var item in property.EnumerateArray())
            {
                if (item.TryGetProperty("lang", out var langElement) && item.TryGetProperty("value", out var valueElement) &&
                    langElement.ValueKind == JsonValueKind.String && valueElement.ValueKind == JsonValueKind.String)
                {
                    list.Add(new LangStringNameType(langElement.GetString(), valueElement.GetString()));
                }
            }

            assignAction(list);
        }

        private void DeserializeLangStringArrayProperty(JsonElement root, string propertyName, Action<List<ILangStringTextType>> assignAction)
        {
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var list = new List<ILangStringTextType>();
            foreach (var item in property.EnumerateArray())
            {
                if (!item.TryGetProperty("lang", out var langElement) || !item.TryGetProperty("value", out var valueElement) ||
                    langElement.ValueKind != JsonValueKind.String || valueElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                // Convert ILangStringNameType to ILangStringTextType if possible
                var convertedItem = ConvertToLangStringTextType(new LangStringNameType(langElement.GetString(), valueElement.GetString()));
                if (convertedItem != null)
                {
                    list.Add(convertedItem);
                }
            }

            assignAction(list);
        }

        private static ILangStringTextType ConvertToLangStringTextType(ILangStringNameType nameType) => new LangStringTextType(nameType.Language, nameType.Text);

        public override void Write(Utf8JsonWriter writer, IReferable value, JsonSerializerOptions options) => throw new NotImplementedException();

        private static IReferable? CreateAdequateType(string elementName)
        {
            var administrationShellType = Enum.GetName(typeof(KeyTypes), KeyTypes.AssetAdministrationShell);
            var conceptDescriptionType  = Enum.GetName(typeof(KeyTypes), KeyTypes.ConceptDescription);
            var submodelType            = Enum.GetName(typeof(KeyTypes), KeyTypes.Submodel);

            if (elementName == administrationShellType)
            {
                return new AssetAdministrationShell(string.Empty, null);
            }

            if (elementName == conceptDescriptionType)
            {
                return new ConceptDescription(string.Empty, null);
            }

            if (elementName == submodelType)
            {
                return new Submodel(string.Empty, null);
            }

            return CreateSubmodelElementInstance(elementName);
        }

        private static ISubmodelElement? CreateSubmodelElementInstance(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null && typeof(ISubmodelElement).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type) as ISubmodelElement;
            }

            return null;
        }
    }

    public class AdaptiveFilterContractResolver : JsonConverter<JsonElement>
    {
        // Properties to configure the behavior of the converter
        public bool AasHasViews         { get; }
        public bool BlobHasValue        { get; }
        public bool SubmodelHasElements { get; }
        public bool SmcHasValue         { get; }
        public bool OpHasVariables      { get; }

        public AdaptiveFilterContractResolver(bool aasHasViews = true, bool blobHasValue = true, bool submodelHasElements = true, bool smcHasValue = true,
                                              bool opHasVariables = true)
        {
            AasHasViews         = aasHasViews;
            BlobHasValue        = blobHasValue;
            SubmodelHasElements = submodelHasElements;
            SmcHasValue         = smcHasValue;
            OpHasVariables      = opHasVariables;
        }

        public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new JsonException("Error while reading JSON.", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}