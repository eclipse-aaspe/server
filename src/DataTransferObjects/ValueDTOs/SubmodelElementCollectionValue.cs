namespace DataTransferObjects.ValueDTOs
{
    public record class SubmodelElementCollectionValue(
        string IdShort,
        List<ISubmodelElementValue>? Value = null)
        : ISubmodelElementValue;
}
