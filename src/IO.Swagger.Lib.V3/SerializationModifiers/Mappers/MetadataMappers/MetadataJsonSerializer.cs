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

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;

using DataTransferObjects.ValueDTOs;
using System.Text.Json.Nodes;
using System;
using DataTransferObjects.MetadataDTOs;
using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;
using System.Collections.Generic;
using IO.Swagger.Lib.V3.Exceptions;
using static AasxServerStandardBib.TimeSeriesPlotting.PlotArguments;

public class MetadataJsonSerializer
{
    internal static JsonNode? ToJsonObject(IMetadataDTO that)
    {
        ArgumentNullException.ThrowIfNull(that);

        if (that is RelationshipElementMetadata relationshipElementMetadata)
        {
            return TransformRelationshipElement(relationshipElementMetadata);
        }
        else if (that is AnnotatedRelationshipElementMetadata annotatedRelationshipElementMetadata)
        {
            return TransformAnnotatedRelationshipElement(annotatedRelationshipElementMetadata);
        }
        else if (that is BasicEventElementMetadata basicEventElementMetadata)
        {
            return TransformBasicEventElement(basicEventElementMetadata);
        }
        else
        {
            return Serialize(that);
        }
    }

    //TODO:jtikekar Rename the method
    private static JsonNode Serialize(IMetadataDTO that)
    {
        var mappedIClass = RequestMetadataMapper.Map(that);

        if (mappedIClass != null)
        {
            var jsonNode = Jsonization.Serialize.ToJsonObject(mappedIClass);
            if (mappedIClass is IBlob or IFile)
            {
                jsonNode.Remove("contentType");
            }
            return jsonNode;
        }
        else
        {
            throw new InvalidSerializationModifierException("metadata", that.GetType().Name);
        }
    }

    private static JsonNode? TransformBasicEventElement(BasicEventElementMetadata that)
    {
        var result = new JsonObject();

        if (that.Extensions != null)
        {
            result["extensions"] = TransformExtensions(that.Extensions);
        }

        if (that.Category != null)
        {
            result["category"] = JsonValue.Create(
                that.Category);
        }

        if (that.IdShort != null)
        {
            result["idShort"] = JsonValue.Create(
                that.IdShort);
        }

        if (that.DisplayName != null)
        {
            result["displayName"] = TransformDisplayName(that.DisplayName);
        }

        if (that.Description != null)
        {
            result["description"] = TransformDescription(that.Description);
        }

        if (that.SemanticId != null)
        {
            result["semanticId"] = TransformReference(that.SemanticId);
        }

        if (that.SupplementalSemanticIds != null)
        {
            result["supplementalSemanticIds"] = TransformReferenceList(that.SupplementalSemanticIds);
        }

        if (that.Qualifiers != null)
        {
            result["qualifiers"] = TransformQualifiers(that.Qualifiers);
        }

        if (that.EmbeddedDataSpecifications != null)
        {
            result["embeddedDataSpecifications"] = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);
        }

        result["direction"] = Jsonization.Serialize.DirectionToJsonValue(
            that.Direction);

        result["state"] = Jsonization.Serialize.StateOfEventToJsonValue(
            that.State);

        if (that.MessageTopic != null)
        {
            result["messageTopic"] = JsonValue.Create(
                that.MessageTopic);
        }

        if (that.MessageBroker != null)
        {
            result["messageBroker"] = TransformReference(that.MessageBroker);
        }

        if (that.LastUpdate != null)
        {
            result["lastUpdate"] = JsonValue.Create(
                that.LastUpdate);
        }

        if (that.MinInterval != null)
        {
            result["minInterval"] = JsonValue.Create(
                that.MinInterval);
        }

        if (that.MaxInterval != null)
        {
            result["maxInterval"] = JsonValue.Create(
                that.MaxInterval);
        }

        result["modelType"] = "BasicEventElement";

        return result;
    }
    private static JsonNode? TransformAnnotatedRelationshipElement(AnnotatedRelationshipElementMetadata that)
    {
        var result = new JsonObject();

        if (that.Extensions != null)
        {
            result["extensions"] = TransformExtensions(that.Extensions);
        }

        if (that.Category != null)
        {
            result["category"] = JsonValue.Create(
                that.Category);
        }

        if (that.IdShort != null)
        {
            result["idShort"] = JsonValue.Create(
                that.IdShort);
        }

        if (that.DisplayName != null)
        {
            result["displayName"] = TransformDisplayName(that.DisplayName);
        }

        if (that.Description != null)
        {
            result["description"] = TransformDescription(that.Description);
        }

        if (that.SemanticId != null)
        {
            result["semanticId"] = TransformReference(that.SemanticId);
        }

        if (that.SupplementalSemanticIds != null)
        {
            result["supplementalSemanticIds"] = TransformReferenceList(that.SupplementalSemanticIds);
        }

        if (that.Qualifiers != null)
        {
            result["qualifiers"] = TransformQualifiers(that.Qualifiers);
        }

        if (that.EmbeddedDataSpecifications != null)
        {
            result["embeddedDataSpecifications"] = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);
        }

        result["modelType"] = "AnnotatedRelationshipElement";

        return result;
    }
    
    private static JsonNode? TransformRelationshipElement(RelationshipElementMetadata that)
    {
        var result = new JsonObject();

        if (that.Extensions != null)
        {
            result["extensions"] = TransformExtensions(that.Extensions);
        }

        if (that.Category != null)
        {
            result["category"] = JsonValue.Create(
                that.Category);
        }

        if (that.IdShort != null)
        {
            result["idShort"] = JsonValue.Create(
                that.IdShort);
        }

        if (that.DisplayName != null)
        {
            result["displayName"] = TransformDisplayName(that.DisplayName);
        }

        if (that.Description != null)
        {
            result["description"] = TransformDescription(that.Description);
        }

        if (that.SemanticId != null)
        {
            result["semanticId"] = TransformReference(that.SemanticId);
        }

        if (that.SupplementalSemanticIds != null)
        {
            result["supplementalSemanticIds"] = TransformReferenceList(that.SupplementalSemanticIds);
        }

        if (that.Qualifiers != null)
        {
            result["qualifiers"] = TransformQualifiers(that.Qualifiers);
        }

        if (that.EmbeddedDataSpecifications != null)
        {
            result["embeddedDataSpecifications"] = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);
        }

        result["modelType"] = "RelationshipElement";

        return result;
    }

    private static JsonArray? TransformExtensions(List<ExtensionDTO> that)
    {
        var arrayExtensions = new JsonArray();
        foreach (var extnsionDto in that)
        {
            var extension = new Extension(extnsionDto.Name, MapReference(extnsionDto.SemanticId), MapReferenceList(extnsionDto.SupplementalSemanticIds), extnsionDto.ValueType, extnsionDto.Value);
            arrayExtensions.Add(Jsonization.Serialize.ToJsonObject(extension));
        }
        return arrayExtensions;
    }

    private static JsonArray? TransformDisplayName(List<LangStringNameTypeDTO> that)
    {
        var arrayDisplayName = new JsonArray();
        foreach (var langStringDto in that)
        {
            var langString = new LangStringNameType(langStringDto.Language, langStringDto.Text);
            arrayDisplayName.Add(Jsonization.Serialize.ToJsonObject(langString));
        }
        return arrayDisplayName;
    }

    private static JsonArray? TransformDescription(List<LangStringTextTypeDTO> that)
    {
        var arrayDescription = new JsonArray();
        foreach (var langStringDto in that)
        {
            var langString = new LangStringTextType(langStringDto.Language, langStringDto.Text);
            arrayDescription.Add(Jsonization.Serialize.ToJsonObject(langString));
        }
        return arrayDescription;
    }

    private static JsonArray? TransformQualifiers(List<QualifierDTO> that)
    {
        var arrayQualifiers = new JsonArray();
        foreach (var qualifierDto in that)
        {
            arrayQualifiers.Add(TransformQualifier(qualifierDto));
        }
        return arrayQualifiers;
    }

    private static JsonNode? TransformQualifier(QualifierDTO that)
    {
        var qualifier = MapQualifier(that);
        return Jsonization.Serialize.ToJsonObject(qualifier);
    }

    private static JsonArray? TransformReferenceList(List<ReferenceDTO> that)
    {
        var arrayReferences = new JsonArray();
        foreach (var referenceDto in that)
        {
            arrayReferences.Add(TransformReference(referenceDto));
        }
        return arrayReferences;
    }

    private static JsonNode? TransformReference(ReferenceDTO that)
    {
        var reference = MapReference(that);
        return Jsonization.Serialize.ToJsonObject(reference);
    }

    private static JsonArray? TransformEmbeddedDataSpecifications(List<EmbeddedDataSpecificationDTO> that)
    {
        var arrayEmbeddedDataSpecifications = new JsonArray();
        foreach (var embeddedDSDto in that)
        {
            arrayEmbeddedDataSpecifications.Add(TransformEmbeddedDataSpecification(embeddedDSDto));
        }
        return arrayEmbeddedDataSpecifications;
    }

    private static JsonNode? TransformEmbeddedDataSpecification(EmbeddedDataSpecificationDTO that)
    {
        var embeddedDS = MapEmbeddedDataSpecification(that);
        return Jsonization.Serialize.ToJsonObject(embeddedDS);
    }

    private static List<IReference> MapReferenceList(List<ReferenceDTO> references)
    {
        if (references == null)
            return null;
        var result = new List<IReference>();
        foreach (var reference in references)
        {
            result.Add(MapReference(reference));
        }

        return result;
    }

    private static IReference MapReference(ReferenceDTO referenceDTO)
    {
        if (referenceDTO == null)
            return null;
        return new Reference(referenceDTO.Type, MapKeys(referenceDTO.Keys), MapReference(referenceDTO.ReferredSemanticId));
    }

    private static List<IKey> MapKeys(List<KeyDTO> keys)
    {
        if (keys == null)
            return null;

        var result = new List<IKey>();
        foreach (var key in keys)
        {
            result.Add(new Key(key.Type, key.Value));
        }

        return result;
    }

    private static IQualifier MapQualifier(QualifierDTO qualifierDTO)
    {
        if (qualifierDTO == null)
            return null;
        return new Qualifier(qualifierDTO.Type, qualifierDTO.ValueType, MapReference(qualifierDTO.SemanticId), MapReferenceList(qualifierDTO.SupplementalSemanticIds), qualifierDTO.Kind, qualifierDTO.Value, MapReference(qualifierDTO.ValueId));
    }

    private static IEmbeddedDataSpecification MapEmbeddedDataSpecification(EmbeddedDataSpecificationDTO embDataSpecDTO)
    {
        if (embDataSpecDTO == null)
            return null;

        //TODO: Map Dataspecification content
        return new EmbeddedDataSpecification(MapReference(embDataSpecDTO.DataSpecification), null);
    }
}
