namespace DataTransferObjects.CommonDTOs
{
    public record class LangStringNameTypeDTO(
            string Language,
            string Text) : IDTO;
}
