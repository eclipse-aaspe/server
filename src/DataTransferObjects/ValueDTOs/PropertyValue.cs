namespace DataTransferObjects.ValueDTOs
{
    public record class PropertyValue(
        string IdShort,
        string? Value = null) : ISubmodelElementValue;
}
