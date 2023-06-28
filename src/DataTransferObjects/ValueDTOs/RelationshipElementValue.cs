using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class RelationshipElementValue(string idShort, ReferenceDTO first, ReferenceDTO second) : ISubmodelElementValue;
}
