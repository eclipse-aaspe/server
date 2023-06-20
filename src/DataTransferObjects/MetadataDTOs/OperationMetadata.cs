using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class OperationMetadata(
            List<ExtensionDTO>? extensions = null,
            string? category = null,
            string? idShort = null,
            List<LangStringNameTypeDTO>? displayName = null,
            List<LangStringTextTypeDTO>? description = null,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            List<QualifierDTO>? qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            List<IMetadataDTO>? inputVariables = null,
            List<IMetadataDTO>? outputVariables = null,
            List<IMetadataDTO>? inoutputVariables = null,
            string modelType = "Operation") : ISubmodelElementMetadata;
}
