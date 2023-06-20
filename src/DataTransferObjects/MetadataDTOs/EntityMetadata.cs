using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class EntityMetadata(
            EntityType entityType,
            List<ExtensionDTO>? extensions = null,
            string? category = null,
            string? idShort = null,
            List<LangStringNameTypeDTO>? displayName = null,
            List<LangStringTextTypeDTO>? description = null,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            List<QualifierDTO>? qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            List<ISubmodelElementMetadata>? statements = null,
            string modelType = "Entity") : ISubmodelElementMetadata;
}
