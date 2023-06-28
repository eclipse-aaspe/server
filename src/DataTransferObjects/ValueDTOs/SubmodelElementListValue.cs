namespace DataTransferObjects.ValueDTOs
{
    public record class SubmodelElementListValue(string idShort, List<ISubmodelElementValue> value) : ISubmodelElementValue;
}
