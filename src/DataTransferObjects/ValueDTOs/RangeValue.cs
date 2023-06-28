namespace DataTransferObjects.ValueDTOs
{
    public record class RangeValue(string idShort, string min, string max) : ISubmodelElementValue;
}
