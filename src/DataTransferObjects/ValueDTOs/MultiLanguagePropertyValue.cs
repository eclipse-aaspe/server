namespace DataTransferObjects.ValueDTOs
{
    public record class MultiLanguagePropertyValue(
        string IdShort,
        List<KeyValuePair<string, string>>? LangStrings = null)
        : ISubmodelElementValue;
}
