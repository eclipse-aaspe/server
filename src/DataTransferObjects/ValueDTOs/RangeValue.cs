namespace DataTransferObjects.ValueDTOs
{
    public record class RangeValue(string idShort, string? min = null, string? max = null) : ISubmodelElementValue;
}
