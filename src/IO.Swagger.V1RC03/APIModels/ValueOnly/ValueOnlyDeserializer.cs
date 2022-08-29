using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal static class ValueOnlyDeserializer
    {
        public static ISubmodelElement DeserializeISubmodelElement(JsonNode jsonNode)
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
                            //This is Multilingual Property
                            output = CreateMultilanguageProperty(item.Key, jsonArray);
                            break;
                        }
                    case JsonObject jsonObject:
                        {
                            if (jsonObject.ContainsKey("min") || jsonObject.ContainsKey("max"))
                            {
                                output = CreateRange(item.Key, jsonObject);
                            }
                            else if (jsonObject.ContainsKey("contentType") && jsonObject.ContainsKey("value"))
                            {
                                output = CreateFile(item.Key, jsonObject);  // TODO: jtikekar Blob
                            }
                            else if (jsonObject.ContainsKey("contentType"))
                            {
                                output = CreateBlob(item.Key, jsonObject); //What is value is present
                            }
                            else if (jsonObject.ContainsKey("first") && jsonObject.ContainsKey("second") && jsonObject.ContainsKey("annotations"))
                            {
                                output = CreateAnnotedRelationshipElement(item.Key, jsonObject);
                            }
                            else if (jsonObject.ContainsKey("first") && jsonObject.ContainsKey("second"))
                            {
                                output = CreateRelationshipElement(item.Key, jsonObject);
                            }
                            else if (jsonObject.ContainsKey("type") && jsonObject.ContainsKey("keys"))
                            {
                                output = CreateReferenceElement(item.Key, jsonObject);
                            }
                            else
                            {
                                //This can be SubmodelElementCollection
                                output = CreateSubmodelElementCollection(item.Key, jsonObject);
                            }
                            break;
                        }
                }

            }

            return output;
        }

        private static ISubmodelElement CreateBlob(string idShort, JsonObject jsonObject)
        {
            string contentType = null;
            var contentTypeNode = jsonObject["contentType"] as JsonValue;
            if (contentTypeNode != null)
            {
                contentTypeNode.TryGetValue(out contentType);
            }
            //TODO jtikekar Support Value from Blob
            //var valueNode = jsonObject["value"] as JsonValue;
            //if (valueNode != null)
            //{
            //    valueNode.TryGetValue(out value);
            //}

            return new Blob(contentType, idShort: idShort);
        }

        private static ISubmodelElement CreateSubmodelElementCollection(string idShort, JsonObject jsonObject)
        {
            var output = new SubmodelElementCollection(idShort: idShort);
            var submodelElements = new List<ISubmodelElement>();

            foreach (var item in jsonObject)
            {
                var newNode = new JsonObject(new[] { KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString())) });
                var submodelElement = DeserializeISubmodelElement(newNode);
                submodelElements.Add(submodelElement);
            }
            output.Value = submodelElements;

            return output;
        }

        private static ISubmodelElement CreateReferenceElement(string idShort, JsonObject jsonObject)
        {
            var output = new ReferenceElement(idShort: idShort);
            Reference reference = Jsonization.Deserialize.ReferenceFrom(jsonObject);
            if (reference != null)
            {
                output.Value = reference;
            }
            return output;
        }

        private static ISubmodelElement CreateAnnotedRelationshipElement(string idShort, JsonObject jsonObject)
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
                    if (annotationNode != null)
                    {
                        var annotation = DeserializeISubmodelElement(annotationNode);
                        annotations.Add((IDataElement)annotation);
                    }
                }
                output.Annotations = annotations;
            }
            return output;
        }

        private static ISubmodelElement CreateRelationshipElement(string idShort, JsonObject jsonObject)
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

        private static ISubmodelElement CreateFile(string idShort, JsonObject jsonObject)
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

        private static ISubmodelElement CreateRange(string idShort, JsonObject jsonObject)
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

        private static ISubmodelElement CreateMultilanguageProperty(string idShort, JsonArray jsonArray)
        {
            var langStrings = new List<LangString>();
            var langStringSet = new LangStringSet(langStrings);
            var enumerator = jsonArray.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current as JsonObject;
                GetPropertyFromJsonObject(item, out string propertyName, out string propertyValue);
                var langString = new LangString(propertyName, propertyValue);
                langStrings.Add(langString);
            }

            return new MultiLanguageProperty(idShort: idShort, value: langStringSet);
        }

        private static void GetPropertyFromJsonObject(JsonObject item, out string propertyName, out string propertyValue)
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

        internal static object DeserializeSubmodel(JsonNode node)
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
