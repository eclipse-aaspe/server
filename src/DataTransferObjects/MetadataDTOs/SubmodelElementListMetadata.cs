using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class SubmodelElementListMetadata(
            AasSubmodelElements TypeValueListElement,
            List<ExtensionDTO>? Extensions = null,
            string? Category = null,
            string? IdShort = null,
            List<LangStringNameTypeDTO>? DisplayName = null,
            List<LangStringTextTypeDTO>? Description = null,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            List<QualifierDTO>? Qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            bool? OrderRelevant = null,
            ReferenceDTO? SemanticIdListElement = null,
            DataTypeDefXsd? ValueTypeListElement = null,
            string ModelType = "SubmodelElementList") : ISubmodelElementMetadata;
}
