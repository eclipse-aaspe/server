using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class RelationshipElementValue(
        string IdShort,
        ReferenceDTO First,
        ReferenceDTO Second) : ISubmodelElementValue;
}
