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

namespace AasxServerDB;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using AasCore.Aas3_0;
using Microsoft.IdentityModel.Tokens;

public static class Serializer
{
    //Move all serialization to own class, which is used by service?
    // --------------------Serialize--------------------
    public static string? SerializeElement<T>(T? element)
    {
        // check if null
        if (element == null)
        {
            return null;
        }

        // convert to JsonNode and string
        if (typeof(T).GetInterface(typeof(IClass).FullName) == null)
        {
            JsonNode jsonNode = element switch
            {
                AasSubmodelElements ele => Jsonization.Serialize.AasSubmodelElementsToJsonValue(ele),
                AssetKind ele => Jsonization.Serialize.AssetKindToJsonValue(ele),
                DataTypeDefXsd ele => Jsonization.Serialize.DataTypeDefXsdToJsonValue(ele),
                DataTypeIec61360 ele => Jsonization.Serialize.DataTypeIec61360ToJsonValue(ele),
                Direction ele => Jsonization.Serialize.DirectionToJsonValue(ele),
                KeyTypes ele => Jsonization.Serialize.KeyTypesToJsonValue(ele),
                EntityType ele => Jsonization.Serialize.EntityTypeToJsonValue(ele),
                ModellingKind ele => Jsonization.Serialize.ModellingKindToJsonValue(ele),
                QualifierKind ele => Jsonization.Serialize.QualifierKindToJsonValue(ele),
                ReferenceTypes ele => Jsonization.Serialize.ReferenceTypesToJsonValue(ele),
                StateOfEvent ele => Jsonization.Serialize.StateOfEventToJsonValue(ele),
                bool ele => JsonValue.Create(ele),
                _ => throw new InvalidOperationException("Unsupported type: " + typeof(T).FullName)
            };
            var text = jsonNode.ToString();
            return text;
        }
        else
        {
            JsonNode jsonNode = Jsonization.Serialize.ToJsonObject((IClass)element);
            var text = jsonNode.ToJsonString();
            return text;
        }
    }

    public static string? SerializeList<T>(List<T>? list)
    {
        // check if null
        if (list == null)
        {
            return null;
        }

        // convert to JsonArray
        var jsonArray = new JsonArray();
        foreach (var element in list)
        {
            if (element == null)
            {
                continue;
            }

            if (typeof(T).GetInterface(typeof(IClass).FullName) == null)
            {
                throw new InvalidOperationException("List of elements that do not descend from interface IClass is not implemented: " + typeof(T).FullName);
            }

            // convert to JsonObject
            var ele = Jsonization.Serialize.ToJsonObject((IClass)element);
            if (ele == null)
            {
                continue;
            }

            // add to JsonArray
            jsonArray.Add(ele);
        }

        // empty JsonArray
        if (jsonArray.Count == 0)
        {
            return null;
        }

        // convert to string
        var text = jsonArray.ToJsonString();
        return text;
    }

    // --------------------Deserialize--------------------
    public static T? DeserializeElement<T>(string? text, bool required = false)
    {
        // check string
        if (text.IsNullOrEmpty() || text is not string textS)
        {
            if (required)
            {
                return DeserializeElementFromJsonNode<T>(null, required);
            }
            return default;
        }

        // add " to create JsonValue
        if (typeof(T).GetInterface(typeof(IClass).FullName) == null)
        {
            text = $"\"{text}\"";
        }

        // convert to JsonNode
        var jsonNode = JsonNode.Parse(text ?? string.Empty);
        if (jsonNode == null)
        {
            throw new InvalidOperationException("Failed to parse JSON.");
        }

        // convert to T
        var element = DeserializeElementFromJsonNode<T>(jsonNode, required);
        return element;
    }

    private static T? DeserializeElementFromJsonNode<T>(JsonNode? jsonNode, bool required = false)
    {
        // check JsonNode
        if (jsonNode == null)
        {
            // T required
            if (required)
            {
                return DeserializeElementRequired<T>();
            }
            return default;
        }

        // convert to T
        // not an IClass descendant
        if (typeof(T).GetInterface(typeof(IClass).FullName) == null)
        {
            if (typeof(T) == typeof(AasSubmodelElements))
            {
                return (T)(object)Jsonization.Deserialize.AasSubmodelElementsFrom(jsonNode);
            }
            else if (typeof(T) == typeof(AssetKind))
            {
                return (T)(object)Jsonization.Deserialize.AssetKindFrom(jsonNode);
            }
            else if (typeof(T) == typeof(DataTypeDefXsd))
            {
                return (T)(object)Jsonization.Deserialize.DataTypeDefXsdFrom(jsonNode);
            }
            else if (typeof(T) == typeof(DataTypeIec61360))
            {
                return (T)(object)Jsonization.Deserialize.DataTypeIec61360From(jsonNode);
            }
            else if (typeof(T) == typeof(Direction))
            {
                return (T)(object)Jsonization.Deserialize.DirectionFrom(jsonNode);
            }
            else if (typeof(T) == typeof(KeyTypes))
            {
                return (T)(object)Jsonization.Deserialize.KeyTypesFrom(jsonNode);
            }
            else if (typeof(T) == typeof(EntityType))
            {
                return (T)(object)Jsonization.Deserialize.EntityTypeFrom(jsonNode);
            }
            else if (typeof(T) == typeof(ModellingKind))
            {
                return (T)(object)Jsonization.Deserialize.ModellingKindFrom(jsonNode);
            }
            else if (typeof(T) == typeof(QualifierKind))
            {
                return (T)(object)Jsonization.Deserialize.QualifierKindFrom(jsonNode);
            }
            else if (typeof(T) == typeof(ReferenceTypes))
            {
                return (T)(object)Jsonization.Deserialize.ReferenceTypesFrom(jsonNode);
            }
            else if (typeof(T) == typeof(StateOfEvent))
            {
                return (T)(object)Jsonization.Deserialize.StateOfEventFrom(jsonNode);
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)bool.Parse(jsonNode.GetValue<string>());
            }
            else
            {
                throw new InvalidOperationException("Unsupported type (not an IClass descendant): " + typeof(T).FullName);
            }
        }

        // IClass descendant
        {
            if (typeof(T) == typeof(IAdministrativeInformation))
            {
                return (T)(object)Jsonization.Deserialize.AdministrativeInformationFrom(jsonNode);
            }
            else if (typeof(T) == typeof(IResource))
            {
                return (T)(object)Jsonization.Deserialize.ResourceFrom(jsonNode);
            }
            else if (typeof(T) == typeof(IReference))
            {
                return (T)(object)Jsonization.Deserialize.ReferenceFrom(jsonNode);
            }
            else if (typeof(T) == typeof(IQualifier))
            {
                return (T)(object)Jsonization.Deserialize.QualifierFrom(jsonNode);
            }
            else if (typeof(T) == typeof(ILangStringNameType))
            {
                return (T)(object)Jsonization.Deserialize.LangStringNameTypeFrom(jsonNode);
            }
            else if (typeof(T) == typeof(ILangStringTextType))
            {
                return (T)(object)Jsonization.Deserialize.LangStringTextTypeFrom(jsonNode);
            }
            else if (typeof(T) == typeof(IEmbeddedDataSpecification))
            {
                return (T)(object)Jsonization.Deserialize.EmbeddedDataSpecificationFrom(jsonNode);
            }
            else if (typeof(T) == typeof(IExtension))
            {
                return (T)(object)Jsonization.Deserialize.ExtensionFrom(jsonNode);
            }
            else if (typeof(T) == typeof(ISpecificAssetId))
            {
                return (T)(object)Jsonization.Deserialize.SpecificAssetIdFrom(jsonNode);
            }
            else
            {
                throw new InvalidOperationException("Unsupported type (IClass descendant): " + typeof(T).FullName);
            }
        }
    }

    private static T DeserializeElementRequired<T>()
    {
        if (typeof(T) == typeof(DataTypeDefXsd))
        {
            return (T)(object)DataTypeDefXsd.String;
        }
        else if (typeof(T) == typeof(EntityType))
        {
            return (T)(object)EntityType.SelfManagedEntity;
        }
        else if (typeof(T) == typeof(IReference))
        {
            return (T)(object)new Reference(ReferenceTypes.ExternalReference, new List<IKey>());
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)true;
        }
        else
        {
            throw new InvalidOperationException("Deserialization of the element failed because it is required and no default handling is implemented.");
        }
    }

    public static List<T>? DeserializeList<T>(string? text, bool required = false)
    {
        // check string
        if (text.IsNullOrEmpty() || text is not string textS)
        {
            if (required)
            {
                return new List<T>();
            }
            return null;
        }

        // convert to JsonArray
        var jsonArray = JsonNode.Parse(textS);
        if (jsonArray == null)
        {
            throw new InvalidOperationException("Failed to parse JSON.");
        }
        if (jsonArray is not JsonArray array)
        {
            throw new InvalidOperationException("JSON is not an array.");
        }

        // convert to list
        var list = new List<T>();
        foreach (var element in array)
        {
            if (element == null)
            {
                continue;
            }

            // convert to T
            var ele = DeserializeElementFromJsonNode<T>(element);
            if (ele == null)
            {
                continue;
            }

            // add to list
            list.Add(ele);
        }

        // empty list
        if (list.Count == 0)
        {
            return null;
        }

        return list;
    }
}

