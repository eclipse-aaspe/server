using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class BasicEventElementValue(string idShort, ReferenceDTO observed) : ISubmodelElementValue;
}
