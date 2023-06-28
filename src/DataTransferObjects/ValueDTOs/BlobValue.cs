namespace DataTransferObjects.ValueDTOs
{
    public record class BlobValue(string idShort, string contentType, byte[] value = null) : ISubmodelElementValue;
}
