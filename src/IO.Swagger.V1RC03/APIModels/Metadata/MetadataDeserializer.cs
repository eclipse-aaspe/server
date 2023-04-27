
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IO.Swagger.V1RC03.APIModels.Metadata
{
    public static class MetadataDeserializer
    {
        internal static object DeserializeISubmodelElement(JsonNode node)
        {
            ISubmodelElement? result = ISubmodelElementFrom(node, out Reporting.Error? error);
            if (error != null)
            {
                throw new Jsonization.Exception(
                    Reporting.GenerateJsonPath(error.PathSegments),
                    error.Cause);
            }
            return result
                ?? throw new System.InvalidOperationException(
                    "Unexpected output null when error is null");
        }

        private static ISubmodelElement ISubmodelElementFrom(JsonNode node, out Reporting.Error error)
        {
            error = null;

            var obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error("Expected JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? modelTypeNode = obj["modelType"];
            if (modelTypeNode == null)
            {
                error = new Reporting.Error("Expected a model type, but none is present");
                return null;
            }
            JsonValue? modelTypeValue = modelTypeNode as JsonValue;
            if (modelTypeValue == null)
            {
                error = new Reporting.Error("Expected JsonValue, " + $"but got {modelTypeNode.GetType()}");
                return null;
            }
            modelTypeValue.TryGetValue<string>(out string? modelType);
            if (modelType == null)
            {
                error = new Reporting.Error("Expected a string, " + $"but the conversion failed from {modelTypeValue}");
                return null;
            }

            switch (modelType)
            {
                case "RelationshipElement":
                    return RelationshipElementFrom(node, out error);
                case "AnnotatedRelationshipElement":
                    return AnnotatedRelationshipElementFrom(node, out error);
                case "BasicEventElement":
                    return BasicEventElementFrom(node, out error);
                case "Blob":
                    return BlobFrom(node, out error);
                case "File":
                    return FileFrom(node, out error);
                case "SubmodelElementList":
                    return SubmodelElementListFrom(node, out error);
                case "SubmodelElementCollection":
                    return SubmodelElementCollectionFrom(node, out error);
                case "Operation":
                case "Entity":
                case "Capability":
                case "MultiLanguageProperty":
                case "Property":
                case "Range":
                case "ReferenceElement":
                    return Jsonization.Deserialize.ISubmodelElementFrom(node);
                default:
                    error = new Reporting.Error(
                        $"Unexpected model type for ISubmodelElement: {modelType}");
                    return null;
            }
        }

        private static ISubmodelElement SubmodelElementListFrom(JsonNode node, out Reporting.Error error)
        {

            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }
                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "category"));
                    return null;
                }
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "idShort"));
                    return null;
                }
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }


            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }



            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "checksum"));
                    return null;
                }
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification>  theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }

            JsonNode? nodeOrderRelevant = obj["orderRelevant"];
            bool? theOrderRelevant = null;
            if (nodeOrderRelevant != null)
            {
                theOrderRelevant = BoolFrom(nodeOrderRelevant, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "orderRelevant"));
                    return null;
                }
                if (theOrderRelevant == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theOrderRelevant null when error is also null");
                }
            }

            JsonNode? nodeValue = obj["value"];
            List<ISubmodelElement>? theValue = null;
            if (nodeValue != null)
            {
                JsonArray? arrayValue = nodeValue as JsonArray;
                if (arrayValue == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeValue.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "value"));
                    return null;
                }
                theValue = new List<ISubmodelElement>(
                    arrayValue.Count);
                int indexValue = 0;
                foreach (JsonNode? item in arrayValue)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexValue));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "value"));
                        return null;
                    }
                    ISubmodelElement? parsedItem = ISubmodelElementFrom(
                        item ?? throw new System.InvalidOperationException(),
                        out error);
                    if (error != null)
                    {
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexValue));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "value"));
                        return null;
                    }
                    theValue.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexValue++;
                }
            }

            JsonNode? nodeSemanticIdListElement = obj["semanticIdListElement"];
            Reference? theSemanticIdListElement = null;
            if (nodeSemanticIdListElement != null)
            {
                theSemanticIdListElement = Jsonization.Deserialize.ReferenceFrom(nodeSemanticIdListElement);
                if (theSemanticIdListElement == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticIdListElement null when error is also null");
                }
            }

            JsonNode? nodeTypeValueListElement = obj["typeValueListElement"];
            if (nodeTypeValueListElement == null)
            {
                error = new Reporting.Error(
                    "Required property \"typeValueListElement\" is missing ");
                return null;
            }
            AasSubmodelElements? theTypeValueListElement = Jsonization.Deserialize.AasSubmodelElementsFrom(nodeTypeValueListElement);
            if (theTypeValueListElement == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected theTypeValueListElement null when error is also null");
            }

            JsonNode? nodeValueTypeListElement = obj["valueTypeListElement"];
            DataTypeDefXsd? theValueTypeListElement = null;
            if (nodeValueTypeListElement != null)
            {
                theValueTypeListElement = Jsonization.Deserialize.DataTypeDefXsdFrom(nodeValueTypeListElement);
                if (theValueTypeListElement == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theValueTypeListElement null when error is also null");
                }
            }

            return new SubmodelElementList(
                theTypeValueListElement
                     ?? throw new System.InvalidOperationException(
                        "Unexpected null, had to be handled before"),
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications,
                theOrderRelevant,
                theValue,
                theSemanticIdListElement,
                theValueTypeListElement);

        }

        private static ISubmodelElement SubmodelElementCollectionFrom(JsonNode node, out Reporting.Error error)
        {
            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }
                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "category"));
                    return null;
                }
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "idShort"));
                    return null;
                }
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }

            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }

            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "checksum"));
                    return null;
                }
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification> theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }

            JsonNode? nodeValue = obj["value"];
            List<ISubmodelElement>? theValue = null;
            if (nodeValue != null)
            {
                JsonArray? arrayValue = nodeValue as JsonArray;
                if (arrayValue == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeValue.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "value"));
                    return null;
                }
                theValue = new List<ISubmodelElement>(
                    arrayValue.Count);
                int indexValue = 0;
                foreach (JsonNode? item in arrayValue)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexValue));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "value"));
                        return null;
                    }
                    ISubmodelElement? parsedItem = ISubmodelElementFrom(item ?? throw new System.InvalidOperationException(), out error);
                    if (error != null)
                    {
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexValue));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "value"));
                        return null;
                    }
                    theValue.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexValue++;
                }
            }

            return new SubmodelElementCollection(
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications,
                theValue);

        }

        private static bool? BoolFrom(
                JsonNode node,
                out Reporting.Error? error)
        {
            error = null;
            JsonValue? value = node as JsonValue;
            if (value == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonValue, but got {node.GetType()}");
                return null;
            }
            bool ok = value.TryGetValue<bool>(out bool result);
            if (!ok)
            {
                error = new Reporting.Error(
                    "Expected a boolean, but the conversion failed " +
                    $"from {value.ToJsonString()}");
                return null;
            }
            return result;
        }

        private static string? StringFrom(JsonNode node, out Reporting.Error? error)
        {
            error = null;
            JsonValue? value = node as JsonValue;
            if (value == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonValue, but got {node.GetType()}");
                return null;
            }
            bool ok = value.TryGetValue<string>(out string? result);
            if (!ok)
            {
                error = new Reporting.Error(
                    "Expected a string, but the conversion failed " +
                    $"from {value.ToJsonString()}");
                return null;
            }
            if (result == null)
            {
                error = new Reporting.Error(
                    "Expected a string, but got a null");
                return null;
            }
            return result;
        }

        private static File FileFrom(JsonNode node, out Reporting.Error error)
        {

            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }

                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }

            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }

            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification> theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }

            //No contentType and Value in case of metadata, hence null
            return new File(
                null,
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications,
                null);

        }

        private static Blob BlobFrom(JsonNode node, out Reporting.Error error)
        {

            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }
                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }

            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }

            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification> theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }


            //No contentType and value in case of metadata, hence assigning null

            return new Blob(
                null,
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications,
                null);

        }

        private static ISubmodelElement BasicEventElementFrom(JsonNode node, out Reporting.Error error)
        {

            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }
                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }

            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }

            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification> theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }

            JsonNode? nodeDirection = obj["direction"];
            if (nodeDirection == null)
            {
                error = new Reporting.Error(
                    "Required property \"direction\" is missing ");
                return null;
            }
            Direction? theDirection = Jsonization.Deserialize.DirectionFrom(nodeDirection);
            if (theDirection == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected theDirection null when error is also null");
            }

            JsonNode? nodeState = obj["state"];
            if (nodeState == null)
            {
                error = new Reporting.Error(
                    "Required property \"state\" is missing ");
                return null;
            }
            StateOfEvent? theState = Jsonization.Deserialize.StateOfEventFrom(nodeState);
            if (theState == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected theState null when error is also null");
            }

            JsonNode? nodeMessageTopic = obj["messageTopic"];
            string? theMessageTopic = null;
            if (nodeMessageTopic != null)
            {
                theMessageTopic = StringFrom(nodeMessageTopic, out error);
                if (theMessageTopic == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theMessageTopic null when error is also null");
                }
            }

            JsonNode? nodeMessageBroker = obj["messageBroker"];
            Reference? theMessageBroker = null;
            if (nodeMessageBroker != null)
            {
                theMessageBroker = Jsonization.Deserialize.ReferenceFrom(nodeMessageBroker);
                if (theMessageBroker == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theMessageBroker null when error is also null");
                }
            }

            JsonNode? nodeLastUpdate = obj["lastUpdate"];
            string? theLastUpdate = null;
            if (nodeLastUpdate != null)
            {
                theLastUpdate = StringFrom(nodeLastUpdate, out error);
                if (theLastUpdate == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theLastUpdate null when error is also null");
                }
            }

            JsonNode? nodeMinInterval = obj["minInterval"];
            string? theMinInterval = null;
            if (nodeMinInterval != null)
            {
                theMinInterval = StringFrom(nodeMinInterval, out error);
                if (theMinInterval == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theMinInterval null when error is also null");
                }
            }

            JsonNode? nodeMaxInterval = obj["maxInterval"];
            string? theMaxInterval = null;
            if (nodeMaxInterval != null)
            {
                theMaxInterval = StringFrom(nodeMaxInterval, out error);
                if (theMaxInterval == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theMaxInterval null when error is also null");
                }
            }

            //No observed in case of metadata, hence creating dummy observed
            return new BasicEventElement(
                null,
                theDirection
                     ?? throw new System.InvalidOperationException(
                        "Unexpected null, had to be handled before"),
                theState
                     ?? throw new System.InvalidOperationException(
                        "Unexpected null, had to be handled before"),
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications,
                theMessageTopic,
                theMessageBroker,
                theLastUpdate,
                theMinInterval,
                theMaxInterval);

        }

        private static ISubmodelElement AnnotatedRelationshipElementFrom(JsonNode node, out Reporting.Error error)
        {

            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }
                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }

            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }

            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification> theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }

            JsonNode? nodeAnnotations = obj["annotations"];
            List<IDataElement>? theAnnotations = null;
            if (nodeAnnotations != null)
            {
                JsonArray? arrayAnnotations = nodeAnnotations as JsonArray;
                if (arrayAnnotations == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeAnnotations.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "annotations"));
                    return null;
                }
                theAnnotations = new List<IDataElement>(
                    arrayAnnotations.Count);
                int indexAnnotations = 0;
                foreach (JsonNode? item in arrayAnnotations)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexAnnotations));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "annotations"));
                        return null;
                    }
                    IDataElement? parsedItem = IDataElementFrom(
                        item ?? throw new System.InvalidOperationException(),
                        out error);
                    if (error != null)
                    {
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexAnnotations));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "annotations"));
                        return null;
                    }
                    theAnnotations.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexAnnotations++;
                }
            }

            //No first and second in metadata, hence create dummy 
            return new AnnotatedRelationshipElement(
                null,
                null,
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications,
                theAnnotations);

        }

        private static IDataElement IDataElementFrom(JsonNode node, out Reporting.Error error)
        {
            error = null;

            var obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    "Expected JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? modelTypeNode = obj["modelType"];
            if (modelTypeNode == null)
            {
                error = new Reporting.Error(
                    "Expected a model type, but none is present");
                return null;
            }
            JsonValue? modelTypeValue = modelTypeNode as JsonValue;
            if (modelTypeValue == null)
            {
                error = new Reporting.Error(
                    "Expected JsonValue, " +
                    $"but got {modelTypeNode.GetType()}");
                return null;
            }
            modelTypeValue.TryGetValue<string>(out string? modelType);
            if (modelType == null)
            {
                error = new Reporting.Error(
                    "Expected a string, " +
                    $"but the conversion failed from {modelTypeValue}");
                return null;
            }

            switch (modelType)
            {
                case "Blob":
                    return BlobFrom(node, out error);
                case "File":
                    return FileFrom(node, out error);
                case "MultiLanguageProperty":
                case "Property":
                case "Range":
                case "ReferenceElement":
                    return Jsonization.Deserialize.IDataElementFrom(node);
                default:
                    error = new Reporting.Error(
                        $"Unexpected model type for IDataElement: {modelType}");
                    return null;
            }
        }

        private static ISubmodelElement RelationshipElementFrom(JsonNode node, out Reporting.Error error)
        {
            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            JsonNode? nodeExtensions = obj["extensions"];
            List<Extension>? theExtensions = null;
            if (nodeExtensions != null)
            {
                JsonArray? arrayExtensions = nodeExtensions as JsonArray;
                if (arrayExtensions == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeExtensions.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "extensions"));
                    return null;
                }
                theExtensions = new List<Extension>(
                    arrayExtensions.Count);
                int indexExtensions = 0;
                foreach (JsonNode? item in arrayExtensions)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexExtensions));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "extensions"));
                        return null;
                    }
                    Extension? parsedItem = Jsonization.Deserialize.ExtensionFrom(item);
                    theExtensions.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexExtensions++;
                }
            }

            JsonNode? nodeCategory = obj["category"];
            string? theCategory = null;
            if (nodeCategory != null)
            {
                theCategory = StringFrom(nodeCategory, out error);
                if (theCategory == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theCategory null when error is also null");
                }
            }

            JsonNode? nodeIdShort = obj["idShort"];
            string? theIdShort = null;
            if (nodeIdShort != null)
            {
                theIdShort = StringFrom(nodeIdShort, out error);
                if (theIdShort == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theIdShort null when error is also null");
                }
            }

            JsonNode? nodeDisplayName = obj["displayName"];
            JsonArray? arrayDisplayName = nodeDisplayName as JsonArray;
            if (arrayDisplayName == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDisplayName.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "displayName"));
                return null;
            }
            List<LangString>? theDisplayName = new List<LangString>(
                arrayDisplayName.Count);
            int indexDisplayName = 0;
            foreach (JsonNode? item in arrayDisplayName)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);
                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDisplayName));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "displayName"));
                    return null;
                }
                theDisplayName.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDisplayName++;
            }

            JsonNode? nodeDescription = obj["description"];
            JsonArray? arrayDescription = nodeDescription as JsonArray;
            if (arrayDescription == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeDescription.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "description"));
                return null;
            }
            List<LangString>? theDescription = new List<LangString>(
                arrayDescription.Count);
            int indexDescription = 0;
            foreach (JsonNode? item in arrayDescription)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                LangString? parsedItem = Jsonization.Deserialize.LangStringFrom(item);

                if (error != null)
                {
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexDescription));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "description"));
                    return null;
                }
                theDescription.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexDescription++;
            }

            JsonNode? nodeChecksum = obj["checksum"];
            string? theChecksum = null;
            if (nodeChecksum != null)
            {
                theChecksum = StringFrom(nodeChecksum, out error);
                if (theChecksum == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theChecksum null when error is also null");
                }
            }

            JsonNode? nodeKind = obj["kind"];
            ModelingKind? theKind = null;
            if (nodeKind != null)
            {
                theKind = Jsonization.Deserialize.ModelingKindFrom(nodeKind);
                if (theKind == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theKind null when error is also null");
                }
            }

            JsonNode? nodeSemanticId = obj["semanticId"];
            Reference? theSemanticId = null;
            if (nodeSemanticId != null)
            {
                theSemanticId = Jsonization.Deserialize.ReferenceFrom(nodeSemanticId);
                if (theSemanticId == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected theSemanticId null when error is also null");
                }
            }

            JsonNode? nodeSupplementalSemanticIds = obj["supplementalSemanticIds"];
            List<Reference>? theSupplementalSemanticIds = null;
            if (nodeSupplementalSemanticIds != null)
            {
                JsonArray? arraySupplementalSemanticIds = nodeSupplementalSemanticIds as JsonArray;
                if (arraySupplementalSemanticIds == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeSupplementalSemanticIds.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "supplementalSemanticIds"));
                    return null;
                }
                theSupplementalSemanticIds = new List<Reference>(
                    arraySupplementalSemanticIds.Count);
                int indexSupplementalSemanticIds = 0;
                foreach (JsonNode? item in arraySupplementalSemanticIds)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexSupplementalSemanticIds));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "supplementalSemanticIds"));
                        return null;
                    }
                    Reference? parsedItem = Jsonization.Deserialize.ReferenceFrom(item);
                    theSupplementalSemanticIds.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexSupplementalSemanticIds++;
                }
            }

            JsonNode? nodeQualifiers = obj["qualifiers"];
            List<Qualifier>? theQualifiers = null;
            if (nodeQualifiers != null)
            {
                JsonArray? arrayQualifiers = nodeQualifiers as JsonArray;
                if (arrayQualifiers == null)
                {
                    error = new Reporting.Error(
                        $"Expected a JsonArray, but got {nodeQualifiers.GetType()}");
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "qualifiers"));
                    return null;
                }
                theQualifiers = new List<Qualifier>(
                    arrayQualifiers.Count);
                int indexQualifiers = 0;
                foreach (JsonNode? item in arrayQualifiers)
                {
                    if (item == null)
                    {
                        error = new Reporting.Error(
                            "Expected a non-null item, but got a null");
                        error.PrependSegment(
                            new Reporting.IndexSegment(
                                indexQualifiers));
                        error.PrependSegment(
                            new Reporting.NameSegment(
                                "qualifiers"));
                        return null;
                    }
                    Qualifier? parsedItem = Jsonization.Deserialize.QualifierFrom(item);
                    theQualifiers.Add(
                        parsedItem
                            ?? throw new System.InvalidOperationException(
                                "Unexpected result null when error is null"));
                    indexQualifiers++;
                }
            }

            JsonNode? nodeEmbeddedDataSpecifications = obj["embeddedDataSpecifications"];
            JsonArray? arrayEmbeddedDataSpecifications = nodeEmbeddedDataSpecifications as JsonArray;
            if (arrayEmbeddedDataSpecifications == null)
            {
                error = new Reporting.Error(
                    $"Expected a JsonArray, but got {nodeEmbeddedDataSpecifications.GetType()}");
                error.PrependSegment(
                    new Reporting.NameSegment(
                        "embeddedDataSpecifications"));
                return null;
            }
            List<EmbeddedDataSpecification> theEmbeddedDataSpecifications = new List<EmbeddedDataSpecification>(
                arrayEmbeddedDataSpecifications.Count);
            int indexEmbeddedDataSpecifications = 0;
            foreach (JsonNode? item in arrayEmbeddedDataSpecifications)
            {
                if (item == null)
                {
                    error = new Reporting.Error(
                        "Expected a non-null item, but got a null");
                    error.PrependSegment(
                        new Reporting.IndexSegment(
                            indexEmbeddedDataSpecifications));
                    error.PrependSegment(
                        new Reporting.NameSegment(
                            "embeddedDataSpecifications"));
                    return null;
                }
                EmbeddedDataSpecification? parsedItem = Jsonization.Deserialize.EmbeddedDataSpecificationFrom(item);
                theEmbeddedDataSpecifications.Add(
                    parsedItem
                        ?? throw new System.InvalidOperationException(
                            "Unexpected result null when error is null"));
                indexEmbeddedDataSpecifications++;
            }

            //No first and second in metadata, hence null
            return new RelationshipElement(
                null,
                null,
                theExtensions,
                theCategory,
                theIdShort,
                theDisplayName,
                theDescription,
                theChecksum,
                theKind,
                theSemanticId,
                theSupplementalSemanticIds,
                theQualifiers,
                theEmbeddedDataSpecifications);
        }
    }
}
