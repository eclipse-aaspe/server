namespace DataTransferObjects.ValueDTOs
{
    public record class MultiLanguagePropertyValue(string idShort, List<KeyValuePair<string, string>> langStrings) : ISubmodelElementValue;
}
