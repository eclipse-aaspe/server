namespace DataTransferObjects.CommonDTOs
{
    public record class LangStringTextTypeDTO(
            string language,
            string text) : IDTO;
}
