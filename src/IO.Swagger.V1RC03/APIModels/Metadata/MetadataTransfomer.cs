
using Nodes = System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Routing;
using static AasCore.Aas3_0_RC02.Jsonization;

namespace IO.Swagger.V1RC03.APIModels.Metadata
{
    internal class MetadataTransfomer : Visitation.AbstractTransformer<Nodes.JsonObject>
    {

        public override JsonObject Transform(Extension that)
        {
            var result = new JsonObject();

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            result["name"] = JsonValue.Create(
                that.Name);

            if (that.ValueType != null)
            {
                // We need to help the static analyzer with a null coalescing.
                DataTypeDefXsd value = that.ValueType
                    ?? throw new System.InvalidOperationException();
                result["valueType"] = Serialize.DataTypeDefXsdToJsonValue(
                    value);
            }

            if (that.Value != null)
            {
                result["value"] = Nodes.JsonValue.Create(
                    that.Value);
            }

            if (that.RefersTo != null)
            {
                result["refersTo"] = Transform(
                    that.RefersTo);
            }

            return result;
        }

        public override Nodes.JsonObject Transform(AdministrativeInformation that)
        {
            var result = new Nodes.JsonObject();

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(
                        Transform(
                            item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            if (that.Version != null)
            {
                result["version"] = Nodes.JsonValue.Create(
                    that.Version);
            }

            if (that.Revision != null)
            {
                result["revision"] = Nodes.JsonValue.Create(
                    that.Revision);
            }

            return result;
        }

        public override Nodes.JsonObject Transform(Qualifier that)
        {
            var result = new Nodes.JsonObject();

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                QualifierKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.QualifierKindToJsonValue(
                    value);
            }

            result["type"] = Nodes.JsonValue.Create(
                that.Type);

            result["valueType"] = Serialize.DataTypeDefXsdToJsonValue(
                that.ValueType);

            if (that.Value != null)
            {
                result["value"] = Nodes.JsonValue.Create(
                    that.Value);
            }

            if (that.ValueId != null)
            {
                result["valueId"] = Transform(
                    that.ValueId);
            }

            return result;
        }

        public override Nodes.JsonObject Transform(AssetAdministrationShell that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Administration != null)
            {
                result["administration"] = Transform(
                    that.Administration);
            }

            result["id"] = Nodes.JsonValue.Create(
                that.Id);

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(
                        Transform(
                            item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            if (that.DerivedFrom != null)
            {
                result["derivedFrom"] = Transform(
                    that.DerivedFrom);
            }

            result["modelType"] = "AssetAdministrationShell";

            return result;
        }

        public override Nodes.JsonObject Transform(AssetInformation that)
        {
            var result = new Nodes.JsonObject();

            result["assetKind"] = Serialize.AssetKindToJsonValue(
                that.AssetKind);

            if (that.GlobalAssetId != null)
            {
                result["globalAssetId"] = Transform(
                    that.GlobalAssetId);
            }

            if (that.SpecificAssetIds != null)
            {
                var arraySpecificAssetIds = new Nodes.JsonArray();
                foreach (SpecificAssetId item in that.SpecificAssetIds)
                {
                    arraySpecificAssetIds.Add(
                        Transform(
                            item));
                }
                result["specificAssetIds"] = arraySpecificAssetIds;
            }

            if (that.DefaultThumbnail != null)
            {
                result["defaultThumbnail"] = Transform(
                    that.DefaultThumbnail);
            }

            return result;
        }

        public override Nodes.JsonObject Transform(Resource that)
        {
            var result = new Nodes.JsonObject();

            result["path"] = Nodes.JsonValue.Create(
                that.Path);

            if (that.ContentType != null)
            {
                result["contentType"] = Nodes.JsonValue.Create(
                    that.ContentType);
            }

            return result;
        }

        public override Nodes.JsonObject Transform(SpecificAssetId that)
        {
            var result = new Nodes.JsonObject();

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            result["name"] = Nodes.JsonValue.Create(
                that.Name);

            result["value"] = Nodes.JsonValue.Create(
                that.Value);

            result["externalSubjectId"] = Transform(
                that.ExternalSubjectId);

            return result;
        }

        public override Nodes.JsonObject Transform(Submodel that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Administration != null)
            {
                result["administration"] = Transform(
                    that.Administration);
            }

            result["id"] = Nodes.JsonValue.Create(
                that.Id);

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "Submodel";

            return result;
        }

        public override Nodes.JsonObject Transform(RelationshipElement that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "RelationshipElement";

            return result;
        }

        public override Nodes.JsonObject Transform(SubmodelElementList that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            if (that.OrderRelevant != null)
            {
                result["orderRelevant"] = Nodes.JsonValue.Create(
                    that.OrderRelevant);
            }

            if (that.SemanticIdListElement != null)
            {
                result["semanticIdListElement"] = Transform(
                    that.SemanticIdListElement);
            }

            result["typeValueListElement"] = Serialize.AasSubmodelElementsToJsonValue(
                that.TypeValueListElement);

            if (that.ValueTypeListElement != null)
            {
                // We need to help the static analyzer with a null coalescing.
                DataTypeDefXsd value = that.ValueTypeListElement
                    ?? throw new System.InvalidOperationException();
                result["valueTypeListElement"] = Serialize.DataTypeDefXsdToJsonValue(
                    value);
            }

            result["modelType"] = "SubmodelElementList";

            return result;
        }

        public override Nodes.JsonObject Transform(SubmodelElementCollection that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "SubmodelElementCollection";

            return result;
        }

        public override Nodes.JsonObject Transform(Property that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["valueType"] = Serialize.DataTypeDefXsdToJsonValue(
                that.ValueType);

            result["modelType"] = "Property";

            return result;
        }

        public override Nodes.JsonObject Transform(MultiLanguageProperty that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "MultiLanguageProperty";

            return result;
        }

        public override Nodes.JsonObject Transform(AasCore.Aas3_0_RC02.Range that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["valueType"] = Serialize.DataTypeDefXsdToJsonValue(
                that.ValueType);

            result["modelType"] = "Range";

            return result;
        }

        public override Nodes.JsonObject Transform(ReferenceElement that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            if (that.Value != null)
            {
                result["value"] = Transform(
                    that.Value);
            }

            result["modelType"] = "ReferenceElement";

            return result;
        }

        public override Nodes.JsonObject Transform(Blob that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "Blob";

            return result;
        }

        public override Nodes.JsonObject Transform(File that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "File";

            return result;
        }

        public override Nodes.JsonObject Transform(AnnotatedRelationshipElement that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "AnnotatedRelationshipElement";

            return result;
        }

        public override Nodes.JsonObject Transform(Entity that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["entityType"] = Serialize.EntityTypeToJsonValue(
                that.EntityType);

            result["modelType"] = "Entity";

            return result;
        }

        public override Nodes.JsonObject Transform(EventPayload that)
        {
            var result = new Nodes.JsonObject();

            result["source"] = Transform(
                that.Source);

            if (that.SourceSemanticId != null)
            {
                result["sourceSemanticId"] = Transform(
                    that.SourceSemanticId);
            }

            result["observableReference"] = Transform(
                that.ObservableReference);

            if (that.ObservableSemanticId != null)
            {
                result["observableSemanticId"] = Transform(
                    that.ObservableSemanticId);
            }

            if (that.Topic != null)
            {
                result["topic"] = Nodes.JsonValue.Create(
                    that.Topic);
            }

            if (that.SubjectId != null)
            {
                result["subjectId"] = Transform(
                    that.SubjectId);
            }

            result["timeStamp"] = Nodes.JsonValue.Create(
                that.TimeStamp);

            if (that.Payload != null)
            {
                result["payload"] = Nodes.JsonValue.Create(
                    that.Payload);
            }

            return result;
        }

        public override Nodes.JsonObject Transform(BasicEventElement that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["direction"] = Serialize.DirectionToJsonValue(
                that.Direction);

            result["state"] = Serialize.StateOfEventToJsonValue(
                that.State);

            if (that.MessageTopic != null)
            {
                result["messageTopic"] = Nodes.JsonValue.Create(
                    that.MessageTopic);
            }

            if (that.MessageBroker != null)
            {
                result["messageBroker"] = Transform(
                    that.MessageBroker);
            }

            if (that.LastUpdate != null)
            {
                result["lastUpdate"] = Nodes.JsonValue.Create(
                    that.LastUpdate);
            }

            if (that.MinInterval != null)
            {
                result["minInterval"] = Nodes.JsonValue.Create(
                    that.MinInterval);
            }

            if (that.MaxInterval != null)
            {
                result["maxInterval"] = Nodes.JsonValue.Create(
                    that.MaxInterval);
            }

            result["modelType"] = "BasicEventElement";

            return result;
        }

        public override Nodes.JsonObject Transform(Operation that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            if (that.InputVariables != null)
            {
                var arrayInputVariables = new Nodes.JsonArray();
                foreach (OperationVariable item in that.InputVariables)
                {
                    arrayInputVariables.Add(
                        Transform(
                            item));
                }
                result["inputVariables"] = arrayInputVariables;
            }

            if (that.OutputVariables != null)
            {
                var arrayOutputVariables = new Nodes.JsonArray();
                foreach (OperationVariable item in that.OutputVariables)
                {
                    arrayOutputVariables.Add(
                        Transform(
                            item));
                }
                result["outputVariables"] = arrayOutputVariables;
            }

            if (that.InoutputVariables != null)
            {
                var arrayInoutputVariables = new Nodes.JsonArray();
                foreach (OperationVariable item in that.InoutputVariables)
                {
                    arrayInoutputVariables.Add(
                        Transform(
                            item));
                }
                result["inoutputVariables"] = arrayInoutputVariables;
            }

            result["modelType"] = "Operation";

            return result;
        }

        public override Nodes.JsonObject Transform(OperationVariable that)
        {
            var result = new Nodes.JsonObject();

            result["value"] = Transform(
                that.Value);

            return result;
        }

        public override Nodes.JsonObject Transform(Capability that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Kind != null)
            {
                // We need to help the static analyzer with a null coalescing.
                ModelingKind value = that.Kind
                    ?? throw new System.InvalidOperationException();
                result["kind"] = Serialize.ModelingKindToJsonValue(
                    value);
            }

            if (that.SemanticId != null)
            {
                result["semanticId"] = Transform(
                    that.SemanticId);
            }

            if (that.SupplementalSemanticIds != null)
            {
                var arraySupplementalSemanticIds = new Nodes.JsonArray();
                foreach (Reference item in that.SupplementalSemanticIds)
                {
                    arraySupplementalSemanticIds.Add(
                        Transform(
                            item));
                }
                result["supplementalSemanticIds"] = arraySupplementalSemanticIds;
            }

            if (that.Qualifiers != null)
            {
                var arrayQualifiers = new Nodes.JsonArray();
                foreach (Qualifier item in that.Qualifiers)
                {
                    arrayQualifiers.Add(
                        Transform(
                            item));
                }
                result["qualifiers"] = arrayQualifiers;
            }

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            result["modelType"] = "Capability";

            return result;
        }

        public override Nodes.JsonObject Transform(ConceptDescription that)
        {
            var result = new Nodes.JsonObject();

            if (that.Extensions != null)
            {
                var arrayExtensions = new Nodes.JsonArray();
                foreach (Extension item in that.Extensions)
                {
                    arrayExtensions.Add(
                        Transform(
                            item));
                }
                result["extensions"] = arrayExtensions;
            }

            if (that.Category != null)
            {
                result["category"] = Nodes.JsonValue.Create(
                    that.Category);
            }

            if (that.IdShort != null)
            {
                result["idShort"] = Nodes.JsonValue.Create(
                    that.IdShort);
            }

            if (that.DisplayName != null)
            {
                result["displayName"] = Transform(
                    that.DisplayName);
            }

            if (that.Description != null)
            {
                result["description"] = Transform(
                    that.Description);
            }

            if (that.Checksum != null)
            {
                result["checksum"] = Nodes.JsonValue.Create(
                    that.Checksum);
            }

            if (that.Administration != null)
            {
                result["administration"] = Transform(
                    that.Administration);
            }

            result["id"] = Nodes.JsonValue.Create(
                that.Id);

            if (that.EmbeddedDataSpecifications != null)
            {
                var arrayEmbeddedDataSpecifications = new Nodes.JsonArray();
                foreach (EmbeddedDataSpecification item in that.EmbeddedDataSpecifications)
                {
                    arrayEmbeddedDataSpecifications.Add(Transform(item));
                }
                result["embeddedDataSpecifications"] = arrayEmbeddedDataSpecifications;
            }

            if (that.IsCaseOf != null)
            {
                var arrayIsCaseOf = new Nodes.JsonArray();
                foreach (Reference item in that.IsCaseOf)
                {
                    arrayIsCaseOf.Add(
                        Transform(
                            item));
                }
                result["isCaseOf"] = arrayIsCaseOf;
            }

            result["modelType"] = "ConceptDescription";

            return result;
        }

        public override Nodes.JsonObject Transform(Reference that)
        {
            var result = new Nodes.JsonObject();

            result["type"] = Serialize.ReferenceTypesToJsonValue(
                that.Type);

            if (that.ReferredSemanticId != null)
            {
                result["referredSemanticId"] = Transform(
                    that.ReferredSemanticId);
            }

            var arrayKeys = new Nodes.JsonArray();
            foreach (Key item in that.Keys)
            {
                arrayKeys.Add(
                    Transform(
                        item));
            }
            result["keys"] = arrayKeys;

            return result;
        }

        public override Nodes.JsonObject Transform(Key that)
        {
            var result = new Nodes.JsonObject();

            result["type"] = Serialize.KeyTypesToJsonValue(
                that.Type);

            result["value"] = Nodes.JsonValue.Create(
                that.Value);

            return result;
        }

        public override Nodes.JsonObject Transform(LangString that)
        {
            var result = new Nodes.JsonObject();

            result["language"] = Nodes.JsonValue.Create(
                that.Language);

            result["text"] = Nodes.JsonValue.Create(
                that.Text);

            return result;
        }

        //public override Nodes.JsonObject Transform(List<LangString> that)
        public Nodes.JsonObject Transform(List<LangString> that)
        {
            var result = new Nodes.JsonObject();

            var arrayLangStrings = new Nodes.JsonArray();
            foreach (LangString item in that)
            {
                arrayLangStrings.Add(
                    Transform(
                        item));
            }
            result["langStrings"] = arrayLangStrings;

            return result;
        }



        public override Nodes.JsonObject Transform(AasCore.Aas3_0_RC02.Environment that)
        {
            var result = new Nodes.JsonObject();

            if (that.AssetAdministrationShells != null)
            {
                var arrayAssetAdministrationShells = new Nodes.JsonArray();
                foreach (AssetAdministrationShell item in that.AssetAdministrationShells)
                {
                    arrayAssetAdministrationShells.Add(
                        Transform(
                            item));
                }
                result["assetAdministrationShells"] = arrayAssetAdministrationShells;
            }

            if (that.Submodels != null)
            {
                var arraySubmodels = new Nodes.JsonArray();
                foreach (Submodel item in that.Submodels)
                {
                    arraySubmodels.Add(
                        Transform(
                            item));
                }
                result["submodels"] = arraySubmodels;
            }

            if (that.ConceptDescriptions != null)
            {
                var arrayConceptDescriptions = new Nodes.JsonArray();
                foreach (ConceptDescription item in that.ConceptDescriptions)
                {
                    arrayConceptDescriptions.Add(
                        Transform(
                            item));
                }
                result["conceptDescriptions"] = arrayConceptDescriptions;
            }

            return result;
        }

        public override Nodes.JsonObject Transform(EmbeddedDataSpecification that)
        {
            var result = new Nodes.JsonObject();

            result["dataSpecification"] = Transform(
                that.DataSpecification);

            result["dataSpecificationContent"] = Transform(
                that.DataSpecificationContent);

            return result;
        }

        public override Nodes.JsonObject Transform(ValueReferencePair that)
        {
            var result = new Nodes.JsonObject();

            result["value"] = Nodes.JsonValue.Create(
                that.Value);

            result["valueId"] = Transform(
                that.ValueId);

            return result;
        }

        public override Nodes.JsonObject Transform(ValueList that)
        {
            var result = new Nodes.JsonObject();

            var arrayValueReferencePairs = new Nodes.JsonArray();
            foreach (ValueReferencePair item in that.ValueReferencePairs)
            {
                arrayValueReferencePairs.Add(
                    Transform(
                        item));
            }
            result["valueReferencePairs"] = arrayValueReferencePairs;

            return result;
        }

        public override Nodes.JsonObject Transform(DataSpecificationIec61360 that)
        {
            var result = new Nodes.JsonObject();

            var arrayPreferredName = new Nodes.JsonArray();
            foreach (LangString item in that.PreferredName)
            {
                arrayPreferredName.Add(
                    Transform(
                        item));
            }
            result["preferredName"] = arrayPreferredName;

            if (that.ShortName != null)
            {
                var arrayShortName = new Nodes.JsonArray();
                foreach (LangString item in that.ShortName)
                {
                    arrayShortName.Add(
                        Transform(
                            item));
                }
                result["shortName"] = arrayShortName;
            }

            if (that.Unit != null)
            {
                result["unit"] = Nodes.JsonValue.Create(
                    that.Unit);
            }

            if (that.UnitId != null)
            {
                result["unitId"] = Transform(
                    that.UnitId);
            }

            if (that.SourceOfDefinition != null)
            {
                result["sourceOfDefinition"] = Nodes.JsonValue.Create(
                    that.SourceOfDefinition);
            }

            if (that.Symbol != null)
            {
                result["symbol"] = Nodes.JsonValue.Create(
                    that.Symbol);
            }

            if (that.DataType != null)
            {
                // We need to help the static analyzer with a null coalescing.
                DataTypeIec61360 value = that.DataType
                    ?? throw new System.InvalidOperationException();
                result["dataType"] = Serialize.DataTypeIec61360ToJsonValue(
                    value);
            }

            if (that.Definition != null)
            {
                var arrayDefinition = new Nodes.JsonArray();
                foreach (LangString item in that.Definition)
                {
                    arrayDefinition.Add(
                        Transform(
                            item));
                }
                result["definition"] = arrayDefinition;
            }

            if (that.ValueFormat != null)
            {
                result["valueFormat"] = Nodes.JsonValue.Create(
                    that.ValueFormat);
            }

            if (that.ValueList != null)
            {
                result["valueList"] = Transform(
                    that.ValueList);
            }

            if (that.Value != null)
            {
                result["value"] = Nodes.JsonValue.Create(
                    that.Value);
            }

            if (that.LevelType != null)
            {
                // We need to help the static analyzer with a null coalescing.
                LevelType value = that.LevelType
                    ?? throw new System.InvalidOperationException();
                result["levelType"] = Serialize.LevelTypeToJsonValue(
                    value);
            }

            result["modelType"] = "DataSpecificationIEC61360";

            return result;
        }

        public override Nodes.JsonObject Transform(DataSpecificationPhysicalUnit that)
        {
            var result = new Nodes.JsonObject();

            result["unitName"] = Nodes.JsonValue.Create(
                that.UnitName);

            result["unitSymbol"] = Nodes.JsonValue.Create(
                that.UnitSymbol);

            var arrayDefinition = new Nodes.JsonArray();
            foreach (LangString item in that.Definition)
            {
                arrayDefinition.Add(
                    Transform(
                        item));
            }
            result["definition"] = arrayDefinition;

            if (that.SiNotation != null)
            {
                result["siNotation"] = Nodes.JsonValue.Create(
                    that.SiNotation);
            }

            if (that.SiName != null)
            {
                result["siName"] = Nodes.JsonValue.Create(
                    that.SiName);
            }

            if (that.DinNotation != null)
            {
                result["dinNotation"] = Nodes.JsonValue.Create(
                    that.DinNotation);
            }

            if (that.EceName != null)
            {
                result["eceName"] = Nodes.JsonValue.Create(
                    that.EceName);
            }

            if (that.EceCode != null)
            {
                result["eceCode"] = Nodes.JsonValue.Create(
                    that.EceCode);
            }

            if (that.NistName != null)
            {
                result["nistName"] = Nodes.JsonValue.Create(
                    that.NistName);
            }

            if (that.SourceOfDefinition != null)
            {
                result["sourceOfDefinition"] = Nodes.JsonValue.Create(
                    that.SourceOfDefinition);
            }

            if (that.ConversionFactor != null)
            {
                result["conversionFactor"] = Nodes.JsonValue.Create(
                    that.ConversionFactor);
            }

            if (that.RegistrationAuthorityId != null)
            {
                result["registrationAuthorityId"] = Nodes.JsonValue.Create(
                    that.RegistrationAuthorityId);
            }

            if (that.Supplier != null)
            {
                result["supplier"] = Nodes.JsonValue.Create(
                    that.Supplier);
            }

            result["modelType"] = "DataSpecificationPhysicalUnit";

            return result;
        }
    }
}
