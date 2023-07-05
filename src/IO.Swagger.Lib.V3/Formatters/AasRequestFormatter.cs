using DataTransferObjects.MetadataDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Formatters
{
    public class AasRequestFormatter : InputFormatter
    {
        public AasRequestFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }

        public override bool CanRead(InputFormatterContext context)
        {
            if (typeof(IClass).IsAssignableFrom(context.ModelType))
            {
                return true;
            }
            else if (typeof(IMetadataDTO).IsAssignableFrom(context.ModelType))
            {
                return true;
            }
            else if (typeof(IValueDTO).IsAssignableFrom(context.ModelType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            Type type = context.ModelType;
            var request = context.HttpContext.Request;
            object result = null;


            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(request.Body).Result;

            if (type == typeof(Submodel))
            {
                result = Jsonization.Deserialize.SubmodelFrom(node);
            }
            else if (type == typeof(AssetAdministrationShell))
            {
                result = Jsonization.Deserialize.AssetAdministrationShellFrom(node);
            }
            else if (type == typeof(SpecificAssetId))
            {
                result = Jsonization.Deserialize.SpecificAssetIdFrom(node);
            }
            else if (type == typeof(ISubmodelElement))
            {
                result = Jsonization.Deserialize.ISubmodelElementFrom(node);
            }
            else if (type == typeof(Reference))
            {
                result = Jsonization.Deserialize.ReferenceFrom(node);
            }
            else if (type == typeof(ConceptDescription))
            {
                result = Jsonization.Deserialize.ConceptDescriptionFrom(node);
            }
            else if (type == typeof(AssetInformation))
            {
                result = Jsonization.Deserialize.AssetInformationFrom(node);
            }
            else if (typeof(SubmodelMetadata).IsAssignableFrom(type))
            {
                result = SubmodelMetadataFrom(node);
            }
            else if (typeof(ISubmodelElementMetadata).IsAssignableFrom(type))
            {
                result = ISubmodelElementMetadataFrom(node);
            }
            else if (typeof(SubmodelValue).IsAssignableFrom(type))
            {
                var serviceProvider = context.HttpContext.RequestServices;
                var valueOnlyJsonDeserializerService = serviceProvider.GetRequiredService<IValueOnlyJsonDeserializer>();
                var encodedSubmodelIdentifier = request.RouteValues["submodelIdentifier"] as string;
                result = valueOnlyJsonDeserializerService.DeserializeSubmodelValue(node, encodedSubmodelIdentifier);
            }
            else if (typeof(IValueDTO).IsAssignableFrom(type))
            {
                var serviceProvider = context.HttpContext.RequestServices;
                var valueOnlyJsonDeserializerService = serviceProvider.GetRequiredService<IValueOnlyJsonDeserializer>();
                var encodedSubmodelIdentifier = request.RouteValues["submodelIdentifier"] as string;
                string idShortPath = (string)request.RouteValues["idShortPath"];
                result = valueOnlyJsonDeserializerService.DeserializeSubmodelElementValue(node, encodedSubmodelIdentifier, idShortPath);
            }

            //Validate modifiers
            //SerializationModifier
            GetSerializationMidifiersFromRequest(context.HttpContext.Request, out LevelEnum level, out ExtentEnum extent);
            SerializationModifiersValidator.Validate(result, level, extent);

            return InputFormatterResult.SuccessAsync(result);

        }

        private void GetSerializationMidifiersFromRequest(HttpRequest request, out LevelEnum level, out ExtentEnum extent)
        {
            request.Query.TryGetValue("level", out StringValues levelValues);
            if (levelValues.Any())
            {
                Enum.TryParse(levelValues.First(), out level);
            }
            else
            {
                level = LevelEnum.Deep;
            }

            request.Query.TryGetValue("extent", out StringValues extenValues);
            if (extenValues.Any())
            {
                Enum.TryParse(extenValues.First(), out extent);
            }
            else
            {
                extent = ExtentEnum.WithoutBlobValue;
            }
        }

        private SubmodelMetadata SubmodelMetadataFrom(JsonNode node)
        {
            SubmodelMetadata output = null;
            //Using newtonsoft json because of known "EnumMemberAttribute" issue (https://github.com/dotnet/runtime/issues/74385) in case of ValueType
            var serilizerSettings = new JsonSerializerSettings();
            serilizerSettings.Converters.Add(new StringEnumConverter());

            var obj = node as JsonObject;
            if (obj == null)
            {
                throw new Exception($"Not a JSON object");
            }
            JsonNode? modelTypeNode = obj["modelType"];
            if (modelTypeNode == null)
            {
                throw new Exception($"No model type found in the request.");
            }
            JsonValue? modelTypeValue = modelTypeNode as JsonValue;
            if (modelTypeValue == null)
            {
                throw new Exception(
                    "Expected JsonValue, " +
                    $"but got {modelTypeNode.GetType()}");
            }
            modelTypeValue.TryGetValue<string>(out string? modelType);
            if (modelType == null)
            {
                throw new Exception(
                    "Expected a string, " +
                    $"but the conversion failed from {modelTypeValue}");
            }

            if (modelType.Equals("submodel", StringComparison.OrdinalIgnoreCase))
            {
                var valueMetadata = ISubmodelElementMetadatListFrom(obj["submodelElements"]);

                node["submodelElements"] = null;
                var submodelMetadata = JsonConvert.DeserializeObject<SubmodelMetadata>(node.ToJsonString(), serilizerSettings);

                output = new SubmodelMetadata(submodelMetadata.id, submodelMetadata.extensions, submodelMetadata.category, submodelMetadata.idShort, submodelMetadata.displayName, submodelMetadata.description, submodelMetadata.administration, submodelMetadata.kind, submodelMetadata.semanticId, submodelMetadata.supplementalSemanticIds, submodelMetadata.qualifiers, submodelMetadata.embeddedDataSpecifications, valueMetadata);
            }

            return output;
        }

        private ISubmodelElementMetadata ISubmodelElementMetadataFrom(JsonNode node)
        {
            ISubmodelElementMetadata output = null;
            //Using newtonsoft json because of known "EnumMemberAttribute" issue (https://github.com/dotnet/runtime/issues/74385) in case of ValueType
            var serilizerSettings = new JsonSerializerSettings();
            serilizerSettings.Converters.Add(new StringEnumConverter());

            var obj = node as JsonObject;
            if (obj == null)
            {
                throw new Exception($"Not a JSON object");
            }
            JsonNode? modelTypeNode = obj["modelType"];
            if (modelTypeNode == null)
            {
                throw new Exception($"No model type found in the request.");
            }
            JsonValue? modelTypeValue = modelTypeNode as JsonValue;
            if (modelTypeValue == null)
            {
                throw new Exception(
                    "Expected JsonValue, " +
                    $"but got {modelTypeNode.GetType()}");
            }
            modelTypeValue.TryGetValue<string>(out string? modelType);
            if (modelType == null)
            {
                throw new Exception(
                    "Expected a string, " +
                    $"but the conversion failed from {modelTypeValue}");
            }

            switch (modelType.ToLower())
            {
                case "property":
                    {
                        //var propertyMetadata = JsonSerializer.Deserialize<PropertyMetadata>(node);
                        output = JsonConvert.DeserializeObject<PropertyMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "annotatedrelationshipelement":
                    {
                        var valueMetadata = ISubmodelElementMetadatListFrom(obj["annotations"]);

                        node["annotations"] = null;
                        var annotatedRelElement = JsonConvert.DeserializeObject<AnnotatedRelationshipElementMetadata>(node.ToJsonString(), serilizerSettings);

                        output = new AnnotatedRelationshipElementMetadata(annotatedRelElement.extensions, annotatedRelElement.category, annotatedRelElement.idShort, annotatedRelElement.displayName, annotatedRelElement.description, annotatedRelElement.semanticId, annotatedRelElement.supplementalSemanticIds, annotatedRelElement.qualifiers, annotatedRelElement.embeddedDataSpecifications, valueMetadata);
                        break;
                    }
                case "basiceventelement":
                    {
                        output = JsonConvert.DeserializeObject<BasicEventElementMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "blob":
                    {
                        output = JsonConvert.DeserializeObject<BlobMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "entity":
                    {
                        var valueMetadata = ISubmodelElementMetadatListFrom(obj["statements"]);

                        node["statements"] = null;
                        var entity = JsonConvert.DeserializeObject<EntityMetadata>(node.ToJsonString(), serilizerSettings);

                        output = new EntityMetadata(entity.entityType, entity.extensions, entity.category, entity.idShort, entity.displayName, entity.description, entity.semanticId, entity.supplementalSemanticIds, entity.qualifiers, entity.embeddedDataSpecifications, valueMetadata);
                        break;
                    }
                case "file":
                    {
                        output = JsonConvert.DeserializeObject<FileMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "multilanguageproperty":
                    {
                        output = JsonConvert.DeserializeObject<MultiLanguagePropertyMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "operation":
                    {
                        output = JsonConvert.DeserializeObject<OperationMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "range":
                    {
                        output = JsonConvert.DeserializeObject<RangeMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "referenceelement":
                    {
                        output = JsonConvert.DeserializeObject<ReferenceElementMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "relationshipelement":
                    {
                        output = JsonConvert.DeserializeObject<RelationshipElementMetadata>(node.ToJsonString(), serilizerSettings);
                        break;
                    }
                case "submodelelementcollection":
                    {

                        var valueMetadata = ISubmodelElementMetadatListFrom(obj["value"]);

                        node["value"] = null;
                        var smeColl = JsonConvert.DeserializeObject<SubmodelElementCollectionMetadata>(node.ToJsonString(), serilizerSettings);

                        output = new SubmodelElementCollectionMetadata(smeColl.extensions, smeColl.category, smeColl.idShort, smeColl.displayName, smeColl.description, smeColl.semanticId, smeColl.supplementalSemanticIds, smeColl.qualifiers, smeColl.embeddedDataSpecifications, valueMetadata);

                        break;
                    }
                case "submodelelementlist":
                    {
                        var valueMetadata = ISubmodelElementMetadatListFrom(obj["value"]);

                        node["value"] = null;
                        var smeList = JsonConvert.DeserializeObject<SubmodelElementListMetadata>(node.ToJsonString(), serilizerSettings);

                        output = new SubmodelElementListMetadata(smeList.typeValueListElement, smeList.extensions, smeList.category, smeList.idShort, smeList.displayName, smeList.description, smeList.semanticId, smeList.supplementalSemanticIds, smeList.qualifiers, smeList.embeddedDataSpecifications, smeList.orderRelevant, smeList.semanticIdListElement, smeList.valueTypeListElement, valueMetadata);
                        break;
                    }
            }

            return output;
        }

        private List<ISubmodelElementMetadata> ISubmodelElementMetadatListFrom(JsonNode jsonNode)
        {
            if (jsonNode == null) return null;

            var valueArray = jsonNode as JsonArray;
            if (valueArray == null)
            {
                throw new Exception(
                    $"Expected a JsonArray, but got {jsonNode.GetType()}");
            }
            var valueMetadata = new List<ISubmodelElementMetadata>(
                valueArray.Count);
            foreach (JsonNode value in valueArray)
            {
                valueMetadata.Add(ISubmodelElementMetadataFrom(value));
            }

            return valueMetadata;
        }
    }
}
