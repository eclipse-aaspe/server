namespace DataTransferObjects.ValueDTOs
{
    public record class BlobValue(
        string IdShort,
        string ContentType,
        byte[]? Value = null) : ISubmodelElementValue;
}
