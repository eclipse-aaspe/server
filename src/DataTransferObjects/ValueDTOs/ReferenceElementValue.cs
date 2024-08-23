using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class ReferenceElementValue(
        string IdShort,
        ReferenceDTO? Value = null) : ISubmodelElementValue;
}
