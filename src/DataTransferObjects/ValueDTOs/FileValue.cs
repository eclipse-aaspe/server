namespace DataTransferObjects.ValueDTOs
{
    public record class FileValue(
        string IdShort,
        string ContentType,
        string? Value = null) : ISubmodelElementValue;
}
