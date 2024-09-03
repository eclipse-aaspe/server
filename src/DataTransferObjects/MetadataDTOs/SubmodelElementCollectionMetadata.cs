using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class SubmodelElementCollectionMetadata(
            List<ExtensionDTO>? Extensions = null,
            string? Category = null,
            string? IdShort = null,
            List<LangStringNameTypeDTO>? DisplayName = null,
            List<LangStringTextTypeDTO>? Description = null,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            List<QualifierDTO>? Qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            string ModelType = "SubmodelElementCollection") : ISubmodelElementMetadata;
}
