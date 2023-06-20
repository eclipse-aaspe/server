using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class AnnotatedRelationshipElementMetadata(
            List<ExtensionDTO>? extensions = null,
            string? category = null,
            string? idShort = null,
            List<LangStringNameTypeDTO>? displayName = null,
            List<LangStringTextTypeDTO>? description = null,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            List<QualifierDTO>? qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            List<ISubmodelElementMetadata>? annotations = null,
            string modelType = "AnnotatedRelationshipElement") : ISubmodelElementMetadata;
}
