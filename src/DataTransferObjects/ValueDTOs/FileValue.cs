namespace DataTransferObjects.ValueDTOs
{
    public record class FileValue(string idShort, string contentType, string value) : ISubmodelElementValue;
}
