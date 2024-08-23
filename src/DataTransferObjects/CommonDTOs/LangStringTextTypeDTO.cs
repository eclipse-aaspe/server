namespace DataTransferObjects.CommonDTOs
{
    public record class LangStringTextTypeDTO(
            string Language,
            string Text) : IDTO;
}
