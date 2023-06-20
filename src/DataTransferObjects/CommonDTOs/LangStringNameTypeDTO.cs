namespace DataTransferObjects.CommonDTOs
{
    public record class LangStringNameTypeDTO(
            string language,
            string text) : IDTO;
}
