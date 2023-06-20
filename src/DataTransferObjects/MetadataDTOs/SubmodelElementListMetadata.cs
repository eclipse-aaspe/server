using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class SubmodelElementListMetadata(
            AasSubmodelElements typeValueListElement,
            List<ExtensionDTO>? extensions = null,
            string? category = null,
            string? idShort = null,
            List<LangStringNameTypeDTO>? displayName = null,
            List<LangStringTextTypeDTO>? description = null,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            List<QualifierDTO>? qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            bool? orderRelevant = null,
            ReferenceDTO? semanticIdListElement = null,
            DataTypeDefXsd? valueTypeListElement = null,
            List<ISubmodelElementMetadata>? value = null,
            string modelType = "SubmodelElementList") : ISubmodelElementMetadata;
}
