using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class ExtensionDTO(
            string Name,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            DataTypeDefXsd? ValueType = null,
            string? Value = null,
            List<ReferenceDTO>? RefersTo = null) : IDTO;
}
