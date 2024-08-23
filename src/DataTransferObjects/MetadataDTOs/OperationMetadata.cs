using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class OperationMetadata(
            List<ExtensionDTO>? Extensions = null,
            string? Category = null,
            string? IdShort = null,
            List<LangStringNameTypeDTO>? DisplayName = null,
            List<LangStringTextTypeDTO>? Description = null,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            List<QualifierDTO>? Qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            List<IMetadataDTO>? InputVariables = null,
            List<IMetadataDTO>? OutputVariables = null,
            List<IMetadataDTO>? InoutputVariables = null,
            string ModelType = "Operation") : ISubmodelElementMetadata;
}
