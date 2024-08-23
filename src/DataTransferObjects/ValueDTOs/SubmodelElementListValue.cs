namespace DataTransferObjects.ValueDTOs
{
    public record class SubmodelElementListValue(
        string IdShort,
        List<ISubmodelElementValue>? Value = null)
        : ISubmodelElementValue;
}
