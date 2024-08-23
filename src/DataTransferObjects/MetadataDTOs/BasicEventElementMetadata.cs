using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class BasicEventElementMetadata(
            Direction Direction,
            StateOfEvent State,
            List<ExtensionDTO>? Extensions = null,
            string? Category = null,
            string? IdShort = null,
            List<LangStringNameTypeDTO>? DisplayName = null,
            List<LangStringTextTypeDTO>? Description = null,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            List<QualifierDTO>? Qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            string? MessageTopic = null,
            ReferenceDTO? MessageBroker = null,
            string? LastUpdate = null,
            string? MinInterval = null,
            string? MaxInterval = null,
            string ModelType = "BasicEventElement") : ISubmodelElementMetadata;

}
