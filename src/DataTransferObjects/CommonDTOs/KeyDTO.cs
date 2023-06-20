using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class KeyDTO(
            KeyTypes type,
            string value) : IDTO;
}
