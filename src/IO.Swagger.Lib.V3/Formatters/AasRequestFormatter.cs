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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Formatters
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using AdminShellNS;

    public class AasRequestFormatter : InputFormatter
    {
        public AasRequestFormatter()
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
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
            Type    type    = context.ModelType;
            var     request = context.HttpContext.Request;
            object? result  = null;

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
                var serviceProvider                  = context.HttpContext.RequestServices;
                var valueOnlyJsonDeserializerService = serviceProvider.GetRequiredService<IValueOnlyJsonDeserializer>();
                var encodedSubmodelIdentifier        = request.RouteValues["submodelIdentifier"] as string;
                result = valueOnlyJsonDeserializerService.DeserializeSubmodelValue(node, encodedSubmodelIdentifier);
            }
            else if (typeof(IValueDTO).IsAssignableFrom(type))
            {
                var    serviceProvider                  = context.HttpContext.RequestServices;
                var    valueOnlyJsonDeserializerService = serviceProvider.GetRequiredService<IValueOnlyJsonDeserializer>();
                var    encodedSubmodelIdentifier        = request.RouteValues["submodelIdentifier"] as string;
                string idShortPath                      = (string)request.RouteValues["idShortPath"];
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

        private SubmodelMetadata? SubmodelMetadataFrom(JsonNode node)
        {
            SubmodelMetadata? output = null;

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!(node is JsonNode obj))
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
                throw new Exception($"Expected JsonValue, but got {modelTypeNode.GetType()}");
            }

            modelTypeValue.TryGetValue<string>(out string? modelType);
            if (modelType == null)
            {
                throw new Exception($"Expected a string, but the conversion failed from {modelTypeValue}");
            }

            if (!modelType.Equals("submodel", StringComparison.OrdinalIgnoreCase))
            {
                return output;
            }

            // Deserialize submodelElements
            var valueMetadata = ISubmodelElementMetadatListFrom(obj["submodelElements"]);

            var jsonString = obj.ToString();

            // Deserialize using System.Text.Json
            var options = new JsonSerializerOptions
                          {
                              Converters = {new JsonStringEnumConverter()} // Assuming you want to convert enums as strings
                          };

            var submodelMetadata = JsonSerializer.Deserialize<SubmodelMetadata>(jsonString, options);

            if (submodelMetadata != null)
            {
                output = new SubmodelMetadata(
                                              submodelMetadata.id,
                                              submodelMetadata.extensions,
                                              submodelMetadata.category,
                                              submodelMetadata.idShort,
                                              submodelMetadata.displayName,
                                              submodelMetadata.description,
                                              submodelMetadata.administration,
                                              submodelMetadata.kind,
                                              submodelMetadata.semanticId,
                                              submodelMetadata.supplementalSemanticIds,
                                              submodelMetadata.qualifiers,
                                              submodelMetadata.embeddedDataSpecifications,
                                              valueMetadata);
            }

            return output;
        }

        public ISubmodelElementMetadata? ISubmodelElementMetadataFrom(JsonNode node)
        {
            ISubmodelElementMetadata? output = null;

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // Using System.Text.Json for JSON handling
            if (!(node is JsonNode obj))
            {
                throw new Exception($"Not a JSON object");
            }

            JsonNode? modelTypeNode = obj["modelType"];
            if (modelTypeNode == null)
            {
                throw new Exception($"No model type found in the request.");
            }

            string modelType = modelTypeNode.ToJsonString();
            if (modelType == null)
            {
                throw new Exception($"Expected a string, but the conversion failed from {modelTypeNode}");
            }

            JsonSerializerOptions serializerOptions = new JsonSerializerOptions
                                                      {
                                                          Converters
                                                              =
                                                              {
                                                                  new AdminShellConverters.JsonAasxConverter("modelType", "name")
                                                              }, // Assuming JsonAasxConverter is compatible with System.Text.Json
                                                          PropertyNameCaseInsensitive = true,
                                                          AllowTrailingCommas         = true,
                                                          ReadCommentHandling         = JsonCommentHandling.Skip,
                                                          DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
                                                      };

            switch (modelType.ToLower())
            {
                case "property":
                {
                    //var propertyMetadata = JsonSerializer.Deserialize<PropertyMetadata>(node);
                    output = JsonSerializer.Deserialize<PropertyMetadata>(node.ToJsonString(), serializerOptions);
                    break;
                }
                case "annotatedrelationshipelement":
                {
                    var valueMetadata = ISubmodelElementMetadatListFrom(obj["annotations"]);

                    node["annotations"] = null;
                    var annotatedRelElement = JsonSerializer.Deserialize<AnnotatedRelationshipElementMetadata>(node.ToJsonString(), serializerOptions);

                    output = new AnnotatedRelationshipElementMetadata(annotatedRelElement.extensions, annotatedRelElement.category, annotatedRelElement.idShort,
                                                                      annotatedRelElement.displayName, annotatedRelElement.description, annotatedRelElement.semanticId,
                                                                      annotatedRelElement.supplementalSemanticIds, annotatedRelElement.qualifiers,
                                                                      annotatedRelElement.embeddedDataSpecifications, valueMetadata);
                    break;
                }
                case "basiceventelement":
                    output = JsonSerializer.Deserialize<BasicEventElementMetadata>(node.ToString(), serializerOptions);
                    break;
                case "blob":
                    output = JsonSerializer.Deserialize<BlobMetadata>(node.ToString(), serializerOptions);
                    break;
                case "entity":
                {
                    var valueMetadata = ISubmodelElementMetadatListFrom(obj["statements"]);

                    node["statements"] = null;
                    var entity = JsonSerializer.Deserialize<EntityMetadata>(node.ToJsonString(), serializerOptions);

                    output = new EntityMetadata(entity.entityType, entity.extensions, entity.category, entity.idShort, entity.displayName, entity.description,
                                                entity.semanticId, entity.supplementalSemanticIds, entity.qualifiers, entity.embeddedDataSpecifications, valueMetadata);
                    break;
                }
                case "file":
                    output = JsonSerializer.Deserialize<FileMetadata>(node.ToString(), serializerOptions);
                    break;
                case "multilanguageproperty":
                    output = JsonSerializer.Deserialize<MultiLanguagePropertyMetadata>(node.ToString(), serializerOptions);
                    break;
                case "operation":
                    output = JsonSerializer.Deserialize<OperationMetadata>(node.ToString(), serializerOptions);
                    break;
                case "range":
                    output = JsonSerializer.Deserialize<RangeMetadata>(node.ToString(), serializerOptions);
                    break;
                case "referenceelement":
                    output = JsonSerializer.Deserialize<ReferenceElementMetadata>(node.ToString(), serializerOptions);
                    break;
                case "relationshipelement":
                    output = JsonSerializer.Deserialize<RelationshipElementMetadata>(node.ToString(), serializerOptions);
                    break;
                case "submodelelementcollection":
                {
                    var valueMetadata = ISubmodelElementMetadatListFrom(obj["value"]);

                    node["value"] = null;
                    var smeColl = JsonSerializer.Deserialize<SubmodelElementCollectionMetadata>(node.ToJsonString(), serializerOptions);

                    output = new SubmodelElementCollectionMetadata(smeColl.extensions, smeColl.category, smeColl.idShort, smeColl.displayName, smeColl.description,
                                                                   smeColl.semanticId, smeColl.supplementalSemanticIds, smeColl.qualifiers, smeColl.embeddedDataSpecifications,
                                                                   valueMetadata);

                    break;
                }
                case "submodelelementlist":
                {
                    var valueMetadata = ISubmodelElementMetadatListFrom(obj["value"]);

                    node["value"] = null;
                    var smeList = JsonSerializer.Deserialize<SubmodelElementListMetadata>(node.ToJsonString(), serializerOptions);

                    output = new SubmodelElementListMetadata(smeList.typeValueListElement, smeList.extensions, smeList.category, smeList.idShort, smeList.displayName,
                                                             smeList.description, smeList.semanticId, smeList.supplementalSemanticIds, smeList.qualifiers,
                                                             smeList.embeddedDataSpecifications, smeList.orderRelevant, smeList.semanticIdListElement,
                                                             smeList.valueTypeListElement, valueMetadata);
                    break;
                }
            }

            return output;
        }


        private List<ISubmodelElementMetadata?> ISubmodelElementMetadatListFrom(JsonNode jsonNode)
        {
            if (jsonNode == null)
            {
                return null;
            }

            if (jsonNode is not JsonArray valueArray)
            {
                throw new Exception(
                                    $"Expected a JsonArray, but got {jsonNode.GetType()}");
            }

            var valueMetadata = new List<ISubmodelElementMetadata?>(
                                                                    valueArray.Count);
            valueMetadata.AddRange(valueArray.Select(value => ISubmodelElementMetadataFrom(value)));

            return valueMetadata;
        }
    }
}