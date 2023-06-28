using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class ReferenceElementValue(string idShort, ReferenceDTO value) : ISubmodelElementValue;
}
