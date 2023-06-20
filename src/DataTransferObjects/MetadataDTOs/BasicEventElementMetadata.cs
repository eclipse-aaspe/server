using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class BasicEventElementMetadata(
            Direction direction,
            StateOfEvent state,
            List<ExtensionDTO>? extensions = null,
            string? category = null,
            string? idShort = null,
            List<LangStringNameTypeDTO>? displayName = null,
            List<LangStringTextTypeDTO>? description = null,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            List<QualifierDTO>? qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            string? messageTopic = null,
            ReferenceDTO? messageBroker = null,
            string? lastUpdate = null,
            string? minInterval = null,
            string? maxInterval = null, string modelType = "BasicEventElement") : ISubmodelElementMetadata;

}
