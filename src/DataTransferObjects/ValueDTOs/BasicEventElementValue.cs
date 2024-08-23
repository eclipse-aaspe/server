using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class BasicEventElementValue(
        string IdShort,
        ReferenceDTO Observed) : ISubmodelElementValue;
}
