namespace DataTransferObjects.ValueDTOs
{
    public record class SubmodelElementCollectionValue(string idShort, List<ISubmodelElementValue> value) : ISubmodelElementValue;
}
