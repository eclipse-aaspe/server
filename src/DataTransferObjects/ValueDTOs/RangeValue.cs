namespace DataTransferObjects.ValueDTOs
{
    public record class RangeValue(
        string IdShort,
        string? Min = null,
        string? Max = null) : ISubmodelElementValue;
}
