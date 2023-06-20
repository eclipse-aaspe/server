using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class ReferenceDTO(
            ReferenceTypes type,
            List<KeyDTO> keys,
            ReferenceDTO? referredSemanticId = null) : IDTO;
}
