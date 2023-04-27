
using AasxServerStandardBib.Logging;
using IO.Swagger.V1RC03.Exceptions;
using IO.Swagger.V1RC03.Services;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    public class ValueOnlyDeserializerService : IValueOnlyDeserializerService
    {
        private IAssetAdministrationShellEnvironmentService _aasEnvService;
        private readonly IBase64UrlDecoderService _decoderService;
        private readonly IAppLogger<ValueOnlyDeserializerService> _logger;

        public void ConfigureAasEnvService(IAssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        public ValueOnlyDeserializerService(IAppLogger<ValueOnlyDeserializerService> logger, IAssetAdministrationShellEnvironmentService aasEnvService, IBase64UrlDecoderService decoderService)
        {
            _logger = logger;
            _aasEnvService = aasEnvService;
            _decoderService = decoderService;
        }
        public ISubmodelElement DeserializeISubmodelElement(JsonNode jsonNode, string encodedSubmodelIdentifier = null, string idShortPath = null)
        {
            ISubmodelElement output = null;
            var obj = jsonNode as JsonObject;

            var enumerator = obj.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                var value = item.Value;

                switch (value)
                {
                    case JsonValue jsonValue:
                        {
                            //This is property
                            jsonValue.TryGetValue(out string propertyValue);
                            output = new Property(DataTypeDefXsd.String, idShort: item.Key, value: propertyValue);
                            break;
                        }
                    case JsonArray jsonArray:
                        {
                            //This is Multilingual Property or SMEList
                            var decodedSubmodelId = _decoderService.Decode("submodelId", encodedSubmodelIdentifier);
                            var element = _aasEnvService.GetSubmodelElementByPathSubmodelRepo(decodedSubmodelId, idShortPath, out _);
                            if (element != null)
                            {
                                if (element is MultiLanguageProperty)
                                {
                                    output = CreateMultilanguageProperty(item.Key, jsonArray);
                                }
                                else if (element is SubmodelElementList smeList)
                                {
                                    output = CreateSubmodelElementList(item.Key, jsonArray, smeList, encodedSubmodelIdentifier, idShortPath);
                                }
                                else
                                {
                                    throw new JsonDeserializationException(item.Key, "Element is not MutlilanguageProperty or SubmodelElementList");
                                }
                            }

                            break;
                        }
                    case JsonObject jsonObject:
                        {
                            if (jsonObject.ContainsKey("min") || jsonObject.ContainsKey("max"))
                            {
                                output = CreateRange(item.Key, jsonObject);
                            }
                            else if (jsonObject.ContainsKey("contentType"))
                            {
                                //If it contains both contentType and Value, both File and Blob are possible. Hence, we need to retrieve actual elements from the server
                                var decodedSubmodelId = _decoderService.Decode("submodelId", encodedSubmodelIdentifier);
                                var element = _aasEnvService.GetSubmodelElementByPathSubmodelRepo(decodedSubmodelId, idShortPath, out _);
                                if (element != null)
                                {
                                    if (element is File)
                                    {
                                        output = CreateFile(item.Key, jsonObject);
                                    }
                                    else if (element is Blob)
                                    {
                                        output = CreateBlob(item.Key, jsonObject);
                                    }
                                    else
                                    {
                                        throw new JsonDeserializationException(item.Key, "Element is not File or Blob");
                                    }
                                }
                            }
                            else if (jsonObject.ContainsKey("first") && jsonObject.ContainsKey("second") && jsonObject.ContainsKey("annotations"))
                            {
                                output = CreateAnnotedRelationshipElement(item.Key, jsonObject, encodedSubmodelIdentifier, idShortPath);
                            }
                            else if (jsonObject.ContainsKey("first") && jsonObject.ContainsKey("second"))
                            {
                                output = CreateRelationshipElement(item.Key, jsonObject);
                            }
                            else if (jsonObject.ContainsKey("type") && jsonObject.ContainsKey("keys"))
                            {
                                output = CreateReferenceElement(item.Key, jsonObject);
                            }
                            else if (jsonObject.ContainsKey("entityType"))
                            {
                                output = CreateEntity(item.Key, jsonObject, encodedSubmodelIdentifier, idShortPath);
                            }
                            else if (jsonObject.ContainsKey("observed"))
                            {
                                output = CreateBasicEventElement(item.Key, jsonObject);
                            }
                            else
                            {
                                //This can be SubmodelElementCollection
                                output = CreateSubmodelElementCollection(item.Key, jsonObject, encodedSubmodelIdentifier, idShortPath);
                            }
                            break;
                        }
                }

            }

            return output;
        }

        private ISubmodelElement CreateSubmodelElementList(string idShort, JsonArray jsonArray, SubmodelElementList smeList, string encodedSubmodelIdentifier, string idShortPath)
        {
            var smeListType = smeList.TypeValueListElement;
            var output = new SubmodelElementList(smeListType, idShort: idShort);
            output.Value = new List<ISubmodelElement>();
            foreach (var item in jsonArray)
            {
                ISubmodelElement submodelElement = null;
                switch (smeListType)
                {
                    case AasSubmodelElements.Property:
                        {
                            var value = item as JsonValue;
                            if (value != null)
                            {
                                value.TryGetValue(out string propertyValue);
                                submodelElement = new Property(DataTypeDefXsd.String, value: propertyValue);
                            }
                            break;
                        }
                    case AasSubmodelElements.MultiLanguageProperty:
                        {
                            var value = item as JsonArray;
                            if (value != null)
                            {
                                submodelElement = CreateMultilanguageProperty("", value);
                            }
                            break;
                        }
                    case AasSubmodelElements.Range:
                        {
                            var value = item as JsonObject;
                            if (value != null)
                            {
                                submodelElement = CreateRange("", value);
                            }
                            break;
                        }
                    case AasSubmodelElements.File:
                        {
                            var value = item as JsonObject;
                            if (value != null)
                            {
                                submodelElement = CreateFile("", value);
                            }
                            break;
                        }
                    case AasSubmodelElements.Blob:
                        {
                            var value = item as JsonObject;
                            if (value != null)
                            {
                                submodelElement = CreateBlob("", value);
                            }
                            break;
                        }
                    case AasSubmodelElements.AnnotatedRelationshipElement:
                        {
                            var value = item as JsonObject;
                            if (value != null)
                            {
                                submodelElement = CreateAnnotedRelationshipElement("", value, encodedSubmodelIdentifier, idShortPath);
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                if (submodelElement != null)
                {
                    output.Value.Add(submodelElement);
                }
            }



            return output;
        }

        private ISubmodelElement CreateBasicEventElement(string idShort, JsonObject jsonObject)
        {
            Reference observed = null;
            var observedNode = jsonObject["observed"] as JsonNode;
            if (observedNode != null)
            {
                observed = Jsonization.Deserialize.ReferenceFrom(observedNode);
            }
            var output = new BasicEventElement(observed, Direction.Input, StateOfEvent.Off, idShort: idShort); //Defining dummy enum values
            return output;
        }

        private ISubmodelElement CreateEntity(string idShort, JsonObject jsonObject, string encodedSubmodelId, string idShortPath)
        {
            string entityType = null;
            var entityTypeNode = jsonObject["entityType"] as JsonValue;
            if (entityTypeNode != null)
            {
                entityTypeNode.TryGetValue(out entityType);
            }

            Entity output = new Entity((EntityType)Stringification.EntityTypeFromString(entityType), idShort: idShort);

            var globalAssetIdNode = jsonObject["globalAssetId"] as JsonNode;
            if (globalAssetIdNode != null)
            {
                var globalAssetId = Jsonization.Deserialize.ReferenceFrom(globalAssetIdNode);
                output.GlobalAssetId = globalAssetId;
            }

            var statementsNode = jsonObject["statements"] as JsonObject;
            if (statementsNode != null)
            {
                var statements = new List<ISubmodelElement>();
                foreach (var item in statementsNode)
                {
                    var newNode = new JsonObject(new[] { KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString())) });
                    var newIdShortPath = idShortPath + "." + item.Key;
                    var statement = DeserializeISubmodelElement(newNode, encodedSubmodelId, newIdShortPath);
                    statements.Add(statement);
                }
                output.Statements = statements;
            }
            return output;
        }

        private ISubmodelElement CreateBlob(string idShort, JsonObject jsonObject)
        {
            string contentType = null;
            var contentTypeNode = jsonObject["contentType"] as JsonValue;
            if (contentTypeNode != null)
            {
                contentTypeNode.TryGetValue(out contentType);
            }

            var blob = new Blob(contentType, idShort: idShort);
            var valueNode = jsonObject["value"] as JsonValue;
            if (valueNode != null)
            {
                valueNode.TryGetValue(out string value);
                blob.Value = System.Convert.FromBase64String(value); ;
            }

            return blob;
        }

        private ISubmodelElement CreateSubmodelElementCollection(string idShort, JsonObject jsonObject, string encodedSubmodelId, string idShortPath)
        {
            var output = new SubmodelElementCollection(idShort: idShort);
            var submodelElements = new List<ISubmodelElement>();

            foreach (var item in jsonObject)
            {
                var newNode = new JsonObject(new[] { KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString())) });
                var newIdShortPath = idShortPath + "." + item.Key;
                var submodelElement = DeserializeISubmodelElement(newNode, encodedSubmodelId, newIdShortPath);
                submodelElements.Add(submodelElement);
            }
            output.Value = submodelElements;

            return output;
        }

        private ISubmodelElement CreateReferenceElement(string idShort, JsonObject jsonObject)
        {
            var output = new ReferenceElement(idShort: idShort);
            Reference reference = Jsonization.Deserialize.ReferenceFrom(jsonObject);
            if (reference != null)
            {
                output.Value = reference;
            }
            return output;
        }

        private ISubmodelElement CreateAnnotedRelationshipElement(string idShort, JsonObject jsonObject, string encodedSubmodelIdentifier, string idShortPath)
        {
            Reference first = null, second = null;
            var firstNode = jsonObject["first"] as JsonNode;
            if (firstNode != null)
            {
                first = Jsonization.Deserialize.ReferenceFrom(firstNode);
            }
            var secondNode = jsonObject["second"] as JsonNode;
            if (secondNode != null)
            {
                second = Jsonization.Deserialize.ReferenceFrom(secondNode);
            }
            var output = new AnnotatedRelationshipElement(first, second, idShort: idShort);

            var annotationsNode = jsonObject["annotations"] as JsonArray;
            if (annotationsNode != null)
            {
                var annotations = new List<IDataElement>();
                foreach (var annotationNode in annotationsNode)
                {
                    var annotation = DeserializeISubmodelElement(annotationNode, encodedSubmodelIdentifier, idShortPath);
                    annotations.Add((IDataElement)annotation);
                }
                output.Annotations = annotations;
            }
            return output;
        }

        private ISubmodelElement CreateRelationshipElement(string idShort, JsonObject jsonObject)
        {
            Reference first = null, second = null;
            var firstNode = jsonObject["first"] as JsonNode;
            if (firstNode != null)
            {
                first = Jsonization.Deserialize.ReferenceFrom(firstNode);
            }
            var secondNode = jsonObject["second"] as JsonNode;
            if (secondNode != null)
            {
                second = Jsonization.Deserialize.ReferenceFrom(secondNode);
            }

            return new RelationshipElement(first, second, idShort: idShort);
        }

        private ISubmodelElement CreateFile(string idShort, JsonObject jsonObject)
        {
            string contentType = null, value = null;
            var contentTypeNode = jsonObject["contentType"] as JsonValue;
            if (contentTypeNode != null)
            {
                contentTypeNode.TryGetValue(out contentType);
            }
            var valueNode = jsonObject["value"] as JsonValue;
            if (valueNode != null)
            {
                valueNode.TryGetValue(out value);
            }

            return new File(contentType, idShort: idShort, value: value);
        }

        private ISubmodelElement CreateRange(string idShort, JsonObject jsonObject)
        {
            string min = null, max = null;
            var minNode = jsonObject["min"] as JsonValue;
            if (minNode != null)
            {
                minNode.TryGetValue(out min);
            }
            var maxNode = jsonObject["max"] as JsonValue;
            if (maxNode != null)
            {
                maxNode.TryGetValue(out max);
            }

            return new AasCore.Aas3_0_RC02.Range(DataTypeDefXsd.String, idShort: idShort, min: min, max: max);
        }

        private ISubmodelElement CreateMultilanguageProperty(string idShort, JsonArray jsonArray)
        {
            var langStrings = new List<LangString>();
            var enumerator = jsonArray.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current as JsonObject;
                GetPropertyFromJsonObject(item, out string propertyName, out string propertyValue);
                var langString = new LangString(propertyName, propertyValue);
                langStrings.Add(langString);
            }

            return new MultiLanguageProperty(idShort: idShort, value: langStrings);
        }

        private void GetPropertyFromJsonObject(JsonObject item, out string propertyName, out string propertyValue)
        {
            propertyName = propertyValue = null;
            var enumerator = item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                propertyName = enumerator.Current.Key;
                var value = enumerator.Current.Value as JsonValue;
                value.TryGetValue(out propertyValue);
            }
        }

        public object DeserializeSubmodel(JsonNode node)
        {
            var submodel = new Submodel("");  //Attached dummy id
            submodel.SubmodelElements = new List<ISubmodelElement>();
            var jsonObject = node as JsonObject;
            foreach (var item in jsonObject)
            {
                var newNode = new JsonObject(new[] { KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString())) });
                var submodelElement = DeserializeISubmodelElement(newNode);
                submodel.SubmodelElements.Add(submodelElement);
            }

            return submodel;
        }
    }
}
