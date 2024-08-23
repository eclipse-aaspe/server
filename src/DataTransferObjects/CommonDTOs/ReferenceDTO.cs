using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class ReferenceDTO(
            ReferenceTypes Type,
            List<KeyDTO> Keys,
            ReferenceDTO? ReferredSemanticId = null) : IDTO;
}
