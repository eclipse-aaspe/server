namespace DataTransferObjects.ValueDTOs
{
    public record class PropertyValue(string idShort, string value) : ISubmodelElementValue;
}
